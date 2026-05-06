// MapLibre GL JS interop module for Needlr.Web. Exposes init / setMarkers / flyTo / dispose.
//
// Design notes
// ============
// The Razor MapComponent owns one map instance per element id. It calls init once on
// first render, setMarkers when the search response changes, and dispose on Dispose().
//
// Pins are rendered as a clustered GeoJSON source rather than individual DOM markers.
// This gives us:
//   - Native MapLibre clustering (the Plateau/Mile End area gets dense; without
//     clustering it's a puddle of overlapping dots).
//   - GPU-rendered circles instead of HTML/CSS — keeps frame rate up under heavy pan.
//   - A single hover popup that swaps content when the user moves between pins,
//     instead of one Marker per pin holding its own listener.
//
// Bounds-changed callbacks are debounced 300ms so the API isn't hit on every cursor
// pixel during a drag.

const handles = new Map(); // id -> { map, popup, debounce, dotnet, pendingPins, styleLoaded }

const SOURCE_ID = 'studios';
const CLUSTER_LAYER_ID = 'studios-clusters';
const CLUSTER_COUNT_LAYER_ID = 'studios-cluster-count';
const POINT_LAYER_ID = 'studios-points';

function ensureMapLibre() {
    if (typeof window.maplibregl === 'undefined') {
        throw new Error('MapLibre GL JS is not loaded. Check index.html script tag.');
    }
}

export function init(elementId, dotnetHelper, opts) {
    ensureMapLibre();
    if (handles.has(elementId)) {
        // Re-init = re-create. Toss the old one.
        dispose(elementId);
    }

    const map = new maplibregl.Map({
        container: elementId,
        // OpenFreeMap Liberty: free, no API key, vector tiles, MapLibre-native. Override per
        // call site by passing opts.styleUrl.
        style: opts?.styleUrl || 'https://tiles.openfreemap.org/styles/liberty',
        center: [opts?.lng ?? -73.5674, opts?.lat ?? 45.5019], // Montréal
        zoom: opts?.zoom ?? 12,
        minZoom: opts?.minZoom ?? 9,
        maxZoom: opts?.maxZoom ?? 18,
        maxBounds: opts?.maxBounds ?? null, // [[swLng, swLat], [neLng, neLat]] or null
        attributionControl: { compact: true },
    });
    map.addControl(new maplibregl.NavigationControl(), 'top-right');

    // Single hover popup, reused. Lighter than creating a Popup per pin.
    const popup = new maplibregl.Popup({
        closeButton: false,
        closeOnClick: false,
        offset: 14,
        className: 'needlr-map-popup-wrap',
    });

    const handle = {
        map,
        popup,
        debounce: null,
        dotnet: dotnetHelper,
        pendingPins: null,    // pins set before style.load — applied once layers exist
        styleLoaded: false,
    };
    handles.set(elementId, handle);

    map.on('load', () => {
        installClusterLayers(map);
        wireInteractions(map, handle);
        handle.styleLoaded = true;
        if (handle.pendingPins) {
            applyPins(handle, handle.pendingPins);
            handle.pendingPins = null;
        }
    });

    const fireBounds = () => {
        if (!handle.dotnet) return;
        const b = map.getBounds();
        const c = map.getCenter();
        try {
            handle.dotnet.invokeMethodAsync('OnBoundsChangedFromJs', {
                southLat: b.getSouth(), westLng: b.getWest(),
                northLat: b.getNorth(), eastLng: b.getEast(),
                centerLat: c.lat, centerLng: c.lng,
            });
        } catch (_) { /* component disposed mid-flight */ }
    };

    map.on('moveend', () => {
        if (handle.debounce) clearTimeout(handle.debounce);
        handle.debounce = setTimeout(fireBounds, 300);
    });
    map.once('load', fireBounds);
}

function installClusterLayers(map) {
    map.addSource(SOURCE_ID, {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
        cluster: true,
        clusterMaxZoom: 14,
        clusterRadius: 50,
    });

    // Cluster blob — sized + tinted by point count. Three-step ramp so the visual hierarchy
    // is readable without being noisy.
    map.addLayer({
        id: CLUSTER_LAYER_ID,
        type: 'circle',
        source: SOURCE_ID,
        filter: ['has', 'point_count'],
        paint: {
            'circle-color': [
                'step', ['get', 'point_count'],
                '#60a5fa',   // 0-9 light
                10, '#2563eb', // 10-29 mid
                30, '#1e3a8a', // 30+ deep
            ],
            'circle-radius': [
                'step', ['get', 'point_count'],
                16,
                10, 22,
                30, 28,
            ],
            'circle-stroke-color': '#fff',
            'circle-stroke-width': 2,
        },
    });

    map.addLayer({
        id: CLUSTER_COUNT_LAYER_ID,
        type: 'symbol',
        source: SOURCE_ID,
        filter: ['has', 'point_count'],
        layout: {
            'text-field': '{point_count_abbreviated}',
            'text-size': 12,
            // Noto Sans Regular ships with OpenFreeMap's glyph endpoint — stays consistent
            // with Liberty's own labels.
            'text-font': ['Noto Sans Regular'],
            'text-allow-overlap': true,
        },
        paint: {
            'text-color': '#fff',
        },
    });

    // Individual studio points. Verified studios get the brand blue so the safety signal
    // pops; unverified default to dark grey.
    map.addLayer({
        id: POINT_LAYER_ID,
        type: 'circle',
        source: SOURCE_ID,
        filter: ['!', ['has', 'point_count']],
        paint: {
            'circle-color': ['case', ['get', 'is_verified'], '#2563eb', '#111827'],
            'circle-radius': 8,
            'circle-stroke-color': '#fff',
            'circle-stroke-width': 2,
        },
    });
}

function wireInteractions(map, handle) {
    // Cluster click → zoom in to the cluster's expansion zoom. MapLibre 4.x switched
    // GeoJSONSource cluster helpers from callback to Promise; v3 callback signature is
    // silently dropped so the map appears unresponsive.
    map.on('click', CLUSTER_LAYER_ID, async (e) => {
        const features = map.queryRenderedFeatures(e.point, { layers: [CLUSTER_LAYER_ID] });
        if (features.length === 0) return;
        const clusterId = features[0].properties.cluster_id;
        try {
            const zoom = await map.getSource(SOURCE_ID).getClusterExpansionZoom(clusterId);
            map.easeTo({ center: features[0].geometry.coordinates, zoom });
        } catch (_) { /* cluster gone (data updated mid-click) */ }
    });

    // Point click → invoke .NET callback with the studio id.
    map.on('click', POINT_LAYER_ID, (e) => {
        const f = e.features?.[0];
        if (!f || !handle.dotnet) return;
        try { handle.dotnet.invokeMethodAsync('OnPinClickedFromJs', f.properties.id); }
        catch (_) { /* component disposed */ }
    });

    // Hover affordances — cursor change + name popup over the point. The popup is a single
    // instance reused across hover targets.
    map.on('mouseenter', POINT_LAYER_ID, (e) => {
        map.getCanvas().style.cursor = 'pointer';
        const f = e.features?.[0];
        if (!f) return;
        handle.popup
            .setLngLat(f.geometry.coordinates)
            .setHTML(buildPopupHtml(f.properties))
            .addTo(map);
    });
    map.on('mouseleave', POINT_LAYER_ID, () => {
        map.getCanvas().style.cursor = '';
        handle.popup.remove();
    });
    map.on('mouseenter', CLUSTER_LAYER_ID, () => { map.getCanvas().style.cursor = 'pointer'; });
    map.on('mouseleave', CLUSTER_LAYER_ID, () => { map.getCanvas().style.cursor = ''; });
}

function escapeHtml(s) {
    return String(s ?? '').replace(/[<>&"]/g, c =>
        ({ '<': '&lt;', '>': '&gt;', '&': '&amp;', '"': '&quot;' }[c]));
}

function buildPopupHtml(props) {
    const verified = props.is_verified
        ? '<span class="needlr-popup-verified">✓ Verified</span>'
        : '';
    return `<div class="needlr-map-popup"><strong>${escapeHtml(props.name)}</strong>${verified}</div>`;
}

function applyPins(handle, pins) {
    const features = (pins ?? []).map(p => ({
        type: 'Feature',
        geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        properties: {
            id: p.id,
            name: p.name,
            is_verified: !!p.isVerified,
        },
    }));
    handle.map.getSource(SOURCE_ID).setData({
        type: 'FeatureCollection',
        features,
    });
}

export function setMarkers(elementId, dotnetHelper, pins) {
    const handle = handles.get(elementId);
    if (!handle) return;
    handle.dotnet = dotnetHelper;
    if (handle.styleLoaded) {
        applyPins(handle, pins);
    } else {
        // Style hasn't finished loading; remember the pins and apply them in the load handler.
        // Without this guard the source/layers wouldn't exist yet and getSource(SOURCE_ID) is null.
        handle.pendingPins = pins;
    }
}

export function flyTo(elementId, lat, lng, zoom) {
    const handle = handles.get(elementId);
    if (!handle) return;
    handle.map.flyTo({ center: [lng, lat], zoom: zoom ?? handle.map.getZoom(), duration: 600 });
}

export function dispose(elementId) {
    const handle = handles.get(elementId);
    if (!handle) return;
    handle.dotnet = null; // stop callbacks
    if (handle.debounce) clearTimeout(handle.debounce);
    handle.popup.remove();
    handle.map.remove();
    handles.delete(elementId);
}

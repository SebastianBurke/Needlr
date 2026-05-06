// MapLibre GL JS interop module for Needlr.Web. Exposes init / setMarkers / dispose.
//
// The Razor MapComponent owns one map instance per element id. It calls init once on
// first render, setMarkers when the search response changes, and dispose on Dispose().
// Bounds-changed callbacks are dispatched to .NET as a fire-and-forget; we debounce so
// the API isn't hit on every cursor pixel during a drag.

const handles = new Map(); // id -> { map, markers: maplibregl.Marker[], debounce, dotnet }

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
        style: opts?.styleUrl || 'https://demotiles.maplibre.org/style.json',
        center: [opts?.lng ?? -73.5674, opts?.lat ?? 45.5019], // Montréal
        zoom: opts?.zoom ?? 12,
        attributionControl: { compact: true },
    });
    map.addControl(new maplibregl.NavigationControl(), 'top-right');

    const handle = {
        map,
        markers: [],
        debounce: null,
        dotnet: dotnetHelper,
    };
    handles.set(elementId, handle);

    const fireBounds = () => {
        if (!handle.dotnet) return;
        const b = map.getBounds();
        const c = map.getCenter();
        const payload = {
            southLat: b.getSouth(), westLng: b.getWest(),
            northLat: b.getNorth(), eastLng: b.getEast(),
            centerLat: c.lat, centerLng: c.lng,
        };
        try {
            handle.dotnet.invokeMethodAsync('OnBoundsChangedFromJs', payload);
        } catch (_) { /* component disposed mid-flight */ }
    };

    const onMoveEnd = () => {
        if (handle.debounce) clearTimeout(handle.debounce);
        handle.debounce = setTimeout(fireBounds, 300);
    };
    map.on('moveend', onMoveEnd);
    map.once('load', fireBounds);
}

export function setMarkers(elementId, dotnetHelper, pins) {
    const handle = handles.get(elementId);
    if (!handle) return;

    // Toss old markers.
    for (const m of handle.markers) m.remove();
    handle.markers = [];

    for (const p of pins ?? []) {
        const el = document.createElement('div');
        el.className = 'needlr-pin' + (p.isVerified ? ' verified' : '');
        el.title = p.name;
        el.addEventListener('click', () => {
            try { dotnetHelper.invokeMethodAsync('OnPinClickedFromJs', p.id); }
            catch (_) { /* component disposed */ }
        });
        const marker = new maplibregl.Marker({ element: el })
            .setLngLat([p.lng, p.lat])
            .addTo(handle.map);
        handle.markers.push(marker);
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
    for (const m of handle.markers) m.remove();
    handle.map.remove();
    handles.delete(elementId);
}

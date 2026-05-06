// Web Push subscription helper invoked from Needlr.Web.Services.PushSubscriptionRegistrar.
// Returns { endpoint, p256dh, auth } on success, null when the browser doesn't support Push
// or the user denies permission.

export async function subscribe(vapidPublicKey) {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) return null;
    const reg = await navigator.serviceWorker.ready;

    // Reuse an existing subscription if the browser already has one for this origin.
    let sub = await reg.pushManager.getSubscription();
    if (!sub) {
        const permission = await Notification.requestPermission();
        if (permission !== 'granted') return null;

        try {
            sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(vapidPublicKey),
            });
        } catch (e) {
            console.warn('Push subscribe failed', e);
            return null;
        }
    }

    const json = sub.toJSON();
    return {
        endpoint: json.endpoint,
        p256dh: json.keys?.p256dh ?? '',
        auth: json.keys?.auth ?? '',
    };
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    const out = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; ++i) out[i] = raw.charCodeAt(i);
    return out;
}

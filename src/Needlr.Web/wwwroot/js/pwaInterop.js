// PWA install-prompt interop. The `beforeinstallprompt` event fires once per page load
// when the browser thinks the site is installable; we stash it so .NET can fire it on
// demand (e.g., after the first confirmed booking per FEATURE_SPECS § PWA install prompt).

let deferredEvent = null;

window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredEvent = e;
});

export function canPrompt() { return deferredEvent !== null; }

export async function prompt() {
    if (!deferredEvent) return null;
    const ev = deferredEvent;
    deferredEvent = null; // Chrome only honors prompt() once per event.
    await ev.prompt();
    const result = await ev.userChoice;
    return result?.outcome ?? null;
}

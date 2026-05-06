// Stripe Elements interop for the booking-request payment-method capture step.
// Phase 18: customer enters card details, Stripe.js returns a paymentMethod.id, the FE
// posts it to /api/bookings as RequestBookingRequest.CustomerPaymentMethodId.
//
// Each instance lives behind a stable elementId that the Razor component owns. We keep
// the Stripe + Elements + PaymentElement objects in a Map so dispose can release them.

const handles = new Map();

function ensureStripe(publishableKey) {
    if (typeof window.Stripe !== 'function') {
        throw new Error('Stripe.js is not loaded. Check index.html script tag.');
    }
    if (!publishableKey) {
        throw new Error('Missing Stripe publishable key.');
    }
    return window.Stripe(publishableKey);
}

export async function mountPaymentElement(elementId, publishableKey) {
    if (handles.has(elementId)) dispose(elementId);
    const stripe = ensureStripe(publishableKey);
    const elements = stripe.elements({
        // setup mode collects a payment method without charging immediately, which
        // matches our pre-auth-then-capture flow.
        mode: 'setup',
        currency: 'cad',
        paymentMethodCreation: 'manual',
    });
    const paymentElement = elements.create('payment', {
        layout: 'tabs',
    });
    paymentElement.mount('#' + elementId);
    handles.set(elementId, { stripe, elements, paymentElement });
}

export async function createPaymentMethod(elementId) {
    const handle = handles.get(elementId);
    if (!handle) throw new Error('Stripe element not mounted: ' + elementId);

    // Validate the form first; Stripe requires this before createPaymentMethod with
    // paymentMethodCreation = manual.
    const { error: submitError } = await handle.elements.submit();
    if (submitError) {
        return { ok: false, error: submitError.message ?? 'Card details invalid.' };
    }

    const { paymentMethod, error } = await handle.stripe.createPaymentMethod({
        elements: handle.elements,
    });
    if (error) {
        return { ok: false, error: error.message ?? 'Failed to create payment method.' };
    }
    return { ok: true, paymentMethodId: paymentMethod.id };
}

export function dispose(elementId) {
    const handle = handles.get(elementId);
    if (!handle) return;
    try { handle.paymentElement.unmount(); } catch (_) { /* already gone */ }
    handles.delete(elementId);
}

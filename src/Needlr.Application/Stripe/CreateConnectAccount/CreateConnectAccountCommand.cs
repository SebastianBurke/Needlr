using Needlr.Application.Messaging;

namespace Needlr.Application.Stripe.CreateConnectAccount;

/// <summary>
/// Creates the calling artist's Stripe Express Connect account (idempotent: if one already
/// exists on the artist record we skip the API call and reuse the id). Sets
/// <c>ArtistPaymentStatus</c> to <c>OnboardingInProgress</c>; the <c>account.updated</c>
/// webhook flips it to <c>Active</c> once Stripe reports KYC complete.
/// </summary>
public sealed record CreateConnectAccountCommand : ICommand<string>;

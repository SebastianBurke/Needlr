using System.Globalization;
using System.Net.Http.Json;
using Needlr.Contracts.Artists;
using Needlr.Contracts.Auth;
using Needlr.Contracts.Availability;
using Needlr.Contracts.Bookings;
using Needlr.Contracts.Client;
using Needlr.Contracts.Common;
using Needlr.Contracts.Discovery;
using Needlr.Contracts.Messaging;
using Needlr.Contracts.Notifications;
using Needlr.Contracts.Portfolio;
using Needlr.Contracts.Studios;
using Needlr.Contracts.TrustSafety;

namespace Needlr.Web.Services;

/// <summary>
/// HttpClient-backed implementation of <see cref="INeedlrApi"/>. Auth endpoints don't carry
/// a bearer token (the bearer handler tries to attach one, but the controller routes are
/// <c>[AllowAnonymous]</c> so an absent token is fine). The same client serves authenticated
/// reads — the bearer handler is configured per-endpoint via the named-client setup in
/// <c>Program.cs</c>; auth + public discovery use the anonymous client, but every read here
/// works either signed-in or signed-out.
/// </summary>
internal sealed class NeedlrApiClient : INeedlrApi
{
    private readonly HttpClient _http;

    public NeedlrApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---- Auth ----

    public Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<LoginRequest, AuthResponse>("api/auth/login", request, cancellationToken);

    public Task<AuthResponse> RegisterCustomerAsync(
        RegisterCustomerRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RegisterCustomerRequest, AuthResponse>(
            "api/auth/register-customer", request, cancellationToken);

    public Task<AuthResponse> RegisterArtistAsync(
        RegisterArtistRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RegisterArtistRequest, AuthResponse>(
            "api/auth/register-artist", request, cancellationToken);

    public Task<AuthResponse> RefreshAsync(
        RefreshTokenRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<RefreshTokenRequest, AuthResponse>(
            "api/auth/refresh", request, cancellationToken);

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/auth/logout", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
    }

    // ---- Discovery ----

    public Task<DiscoveryPageResponse> SearchStudiosAsync(
        DiscoverySearchArgs args, CancellationToken cancellationToken = default)
    {
        var inv = CultureInfo.InvariantCulture;
        var qs = new List<string>
        {
            $"southLat={args.SouthLat.ToString(inv)}",
            $"westLng={args.WestLng.ToString(inv)}",
            $"northLat={args.NorthLat.ToString(inv)}",
            $"eastLng={args.EastLng.ToString(inv)}",
            $"centerLat={args.CenterLat.ToString(inv)}",
            $"centerLng={args.CenterLng.ToString(inv)}",
            $"verifiedOnly={(args.VerifiedOnly ? "true" : "false")}",
            $"acceptingNewBookings={(args.AcceptingNewBookingsOnly ? "true" : "false")}",
            $"sort={Uri.EscapeDataString(args.Sort)}",
            $"page={args.Page}",
            $"pageSize={args.PageSize}",
        };
        if (args.StyleIds is { Count: > 0 })
        {
            foreach (var sid in args.StyleIds)
                qs.Add($"styleIds={sid}");
        }
        if (args.AvailabilityFrom is { } from)
            qs.Add($"availabilityFrom={from:yyyy-MM-dd}");
        if (args.AvailabilityTo is { } to)
            qs.Add($"availabilityTo={to:yyyy-MM-dd}");

        return GetAndDeserializeAsync<DiscoveryPageResponse>(
            $"api/discovery/studios?{string.Join('&', qs)}", cancellationToken);
    }

    // ---- Studios ----

    public Task<StudioResponse> GetStudioAsync(Guid studioId, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<StudioResponse>($"api/studios/{studioId}", cancellationToken);

    public Task<StudioRosterResponse> GetStudioRosterAsync(
        Guid studioId, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<StudioRosterResponse>(
            $"api/studios/{studioId}/roster", cancellationToken);

    // ---- Artists + portfolio ----

    public Task<ArtistDetailResponse> GetArtistAsync(
        Guid artistId, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<ArtistDetailResponse>($"api/artists/{artistId}", cancellationToken);

    public Task<PagedPortfolioResponse> GetArtistPortfolioAsync(
        Guid artistId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<PagedPortfolioResponse>(
            $"api/portfolio/artists/{artistId}?page={page}&pageSize={pageSize}",
            cancellationToken);

    public Task<PortfolioPieceResponse> GetPortfolioPieceAsync(
        Guid pieceId, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<PortfolioPieceResponse>(
            $"api/portfolio/pieces/{pieceId}", cancellationToken);

    // ---- Bookings (Phase 18) ----

    public async Task<Guid> RequestBookingAsync(
        RequestBookingRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<RequestBookingRequest, CreatedIdResponse>(
            "api/bookings", request, cancellationToken);
        return created.Id;
    }

    public Task<BookingPageResponse> ListMyBookingsAsCustomerAsync(
        string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<BookingPageResponse>(
            BuildBookingListUrl("api/bookings/mine/customer", status, page, pageSize),
            cancellationToken);

    public Task<BookingPageResponse> ListMyBookingsAsArtistAsync(
        string? status = null, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<BookingPageResponse>(
            BuildBookingListUrl("api/bookings/mine/artist", status, page, pageSize),
            cancellationToken);

    public Task<BookingDetailResponse> GetBookingAsync(
        Guid bookingId, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<BookingDetailResponse>($"api/bookings/{bookingId}", cancellationToken);

    public async Task AcceptBookingAsync(
        Guid bookingId, AcceptBookingRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/bookings/{bookingId}/accept", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task DeclineBookingAsync(
        Guid bookingId, DeclineBookingRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync($"api/bookings/{bookingId}/decline", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RequestMoreInfoAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/bookings/{bookingId}/request-info", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RespondWithMoreInfoAsync(
        Guid bookingId, RespondWithMoreInfoRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/bookings/{bookingId}/respond-info", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task MarkBookingInProgressAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/bookings/{bookingId}/in-progress", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task MarkBookingCompletedAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/bookings/{bookingId}/complete", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<CancelBookingResponse> CancelBookingByCustomerAsync(
        Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/bookings/{bookingId}/cancel-customer", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
        var body = await resp.Content.ReadFromJsonAsync<CancelBookingResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException((int)resp.StatusCode, null, "Empty response body.");
        return body;
    }

    public async Task<CancelBookingResponse> CancelBookingByArtistAsync(
        Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/bookings/{bookingId}/cancel-artist", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
        var body = await resp.Content.ReadFromJsonAsync<CancelBookingResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException((int)resp.StatusCode, null, "Empty response body.");
        return body;
    }

    public async Task<Guid> SubmitBookingFeedbackAsync(
        Guid bookingId, SubmitBookingFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<SubmitBookingFeedbackRequest, CreatedIdResponse>(
            $"api/bookings/{bookingId}/feedback", request, cancellationToken);
        return created.Id;
    }

    // ---- Messaging (Phase 19) ----

    public Task<ThreadPageResponse> ListMyActiveThreadsAsync(
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<ThreadPageResponse>(
            $"api/threads/mine?page={page}&pageSize={pageSize}", cancellationToken);

    public Task<MessagePageResponse> ListThreadMessagesAsync(
        Guid threadId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<MessagePageResponse>(
            $"api/threads/{threadId}/messages?page={page}&pageSize={pageSize}", cancellationToken);

    public async Task<Guid> SendMessageAsync(
        Guid threadId, SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<SendMessageRequest, CreatedIdResponse>(
            $"api/threads/{threadId}/messages", request, cancellationToken);
        return created.Id;
    }

    public async Task MarkMessageReadAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync($"api/messages/{messageId}/read", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<Guid> ReportMessageAsync(
        Guid messageId, ReportMessageRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<ReportMessageRequest, CreatedIdResponse>(
            $"api/messages/{messageId}/report", request, cancellationToken);
        return created.Id;
    }

    public async Task<int> GetUnreadMessageCountAsync(CancellationToken cancellationToken = default)
    {
        var body = await GetAndDeserializeAsync<UnreadCountResponse>(
            "api/messages/unread-count", cancellationToken);
        return body.Count;
    }

    private static string BuildBookingListUrl(string path, string? status, int page, int pageSize)
    {
        var qs = new List<string> { $"page={page}", $"pageSize={pageSize}" };
        if (!string.IsNullOrEmpty(status)) qs.Add($"status={Uri.EscapeDataString(status)}");
        return $"{path}?{string.Join('&', qs)}";
    }

    // ---- Artist tooling (Phase 20) ----

    public Task<ConnectAccountResponse> CreateConnectAccountAsync(CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<object, ConnectAccountResponse>(
            "api/artists/me/connect-account", new { }, cancellationToken);

    public Task<OnboardingLinkResponse> GenerateOnboardingLinkAsync(
        OnboardingLinkRequest request, CancellationToken cancellationToken = default) =>
        PostAndDeserializeAsync<OnboardingLinkRequest, OnboardingLinkResponse>(
            "api/artists/me/onboarding-link", request, cancellationToken);

    public Task<AvailabilityPatternResponse> GetMyAvailabilityPatternAsync(
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<AvailabilityPatternResponse>("api/availability/pattern", cancellationToken);

    public async Task SetMyAvailabilityPatternAsync(
        SetAvailabilityPatternRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PutAsJsonAsync("api/availability/pattern", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public Task<AvailabilityOverridesResponse> ListMyAvailabilityOverridesAsync(
        DateOnly? from = null, DateOnly? to = null, CancellationToken cancellationToken = default)
    {
        var qs = new List<string>();
        if (from is { } f) qs.Add($"from={f:yyyy-MM-dd}");
        if (to is { } t) qs.Add($"to={t:yyyy-MM-dd}");
        var url = qs.Count == 0 ? "api/availability/overrides" : $"api/availability/overrides?{string.Join('&', qs)}";
        return GetAndDeserializeAsync<AvailabilityOverridesResponse>(url, cancellationToken);
    }

    public async Task AddMyAvailabilityOverrideAsync(
        AddAvailabilityOverrideRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsJsonAsync("api/availability/overrides", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RemoveMyAvailabilityOverrideAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var resp = await _http.DeleteAsync($"api/availability/overrides/{date:yyyy-MM-dd}", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public Task<LeadTimesResponse> GetMyLeadTimesAsync(CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<LeadTimesResponse>("api/availability/lead-times", cancellationToken);

    public async Task SetMyLeadTimesAsync(
        SetLeadTimesRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PutAsJsonAsync("api/availability/lead-times", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<IcalFeedResponse> RotateIcalTokenAsync(CancellationToken cancellationToken = default)
    {
        var resp = await _http.PostAsync("api/availability/ical/rotate", content: null, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
        var body = await resp.Content.ReadFromJsonAsync<IcalFeedResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException((int)resp.StatusCode, null, "Empty response body.");
        return body;
    }

    public async Task<Guid> CreateStudioAsync(
        CreateStudioRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<CreateStudioRequest, CreatedIdResponse>(
            "api/studios", request, cancellationToken);
        return created.Id;
    }

    // ---- Customer tooling (Phase 21) ----

    public async Task<Guid> UploadHealedPhotoAsync(
        Guid bookingId,
        Stream content,
        string contentType,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        form.Add(fileContent, "file", fileName);

        var resp = await _http.PostAsync(
            $"api/portfolio/healed-photos/{bookingId}", form, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
        var body = await resp.Content.ReadFromJsonAsync<CreatedIdResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException((int)resp.StatusCode, null, "Empty response body.");
        return body.Id;
    }

    public Task<NotificationPreferencesResponse> GetMyNotificationPreferencesAsync(
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<NotificationPreferencesResponse>(
            "api/notifications/preferences", cancellationToken);

    public async Task UpdateMyNotificationPreferencesAsync(
        UpdateNotificationPreferencesRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await _http.PutAsJsonAsync("api/notifications/preferences", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<Guid> RegisterPushSubscriptionAsync(
        RegisterPushSubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<RegisterPushSubscriptionRequest, CreatedIdResponse>(
            "api/notifications/push-subscriptions", request, cancellationToken);
        return created.Id;
    }

    public async Task UnregisterPushSubscriptionAsync(
        Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var resp = await _http.DeleteAsync(
            $"api/notifications/push-subscriptions/{subscriptionId}", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    // ---- internals ----

    private async Task<TResponse> PostAndDeserializeAsync<TRequest, TResponse>(
        string path, TRequest request, CancellationToken cancellationToken)
    {
        var response = await _http.PostAsJsonAsync(path, request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException(
                (int)response.StatusCode, null, "Empty response body.");
        return body;
    }

    private async Task<TResponse> GetAndDeserializeAsync<TResponse>(
        string path, CancellationToken cancellationToken)
    {
        var response = await _http.GetAsync(path, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException(
                (int)response.StatusCode, null, "Empty response body.");
        return body;
    }

    private static async Task EnsureSuccessOrThrowAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;

        // Try to parse the API's standard error envelope; fall back to the raw status.
        ApiErrorResponse? apiError = null;
        try
        {
            apiError = await response.Content.ReadFromJsonAsync<ApiErrorResponse>(cancellationToken: cancellationToken);
        }
        catch
        {
            // Non-JSON body — e.g., 502 from upstream proxy. Fall through.
        }

        var first = apiError?.Errors.Count > 0 ? apiError.Errors[0] : null;
        var message = first?.Message
            ?? $"Request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.";
        throw new NeedlrApiException((int)response.StatusCode, first?.Code, message);
    }
}

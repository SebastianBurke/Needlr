using System.Globalization;
using System.Net.Http.Headers;
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
using Needlr.Contracts.Verification;

namespace Needlr.Web.Services;

/// <summary>
/// HttpClient-backed implementation of <see cref="INeedlrApi"/>. Bearer attachment is
/// done per-request inside <see cref="SendAsync"/> rather than via a delegating handler —
/// the published WASM build silently drops the handler from the pipeline (see history of
/// the bearer-attach v1-blocking bug), so explicit attachment here is the durable fix.
/// Anonymous endpoints (login, register, refresh, public discovery, public artist /
/// studio detail) are unaffected: when <see cref="AuthState"/> has no token,
/// <see cref="SendAsync"/> sends the request without an Authorization header.
/// </summary>
internal sealed class NeedlrApiClient : INeedlrApi
{
    private readonly HttpClient _http;
    private readonly AuthState _auth;

    public NeedlrApiClient(HttpClient http, AuthState auth)
    {
        _http = http;
        _auth = auth;
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
        var response = await PostJsonAsync("api/auth/logout", request, cancellationToken);
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
        var resp = await PostJsonAsync($"api/bookings/{bookingId}/accept", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task DeclineBookingAsync(
        Guid bookingId, DeclineBookingRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await PostJsonAsync($"api/bookings/{bookingId}/decline", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RequestMoreInfoAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync($"api/bookings/{bookingId}/request-info", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RespondWithMoreInfoAsync(
        Guid bookingId, RespondWithMoreInfoRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await PostJsonAsync(
            $"api/bookings/{bookingId}/respond-info", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task MarkBookingInProgressAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync($"api/bookings/{bookingId}/in-progress", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task MarkBookingCompletedAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync($"api/bookings/{bookingId}/complete", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<CancelBookingResponse> CancelBookingByCustomerAsync(
        Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync($"api/bookings/{bookingId}/cancel-customer", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
        var body = await resp.Content.ReadFromJsonAsync<CancelBookingResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException((int)resp.StatusCode, null, "Empty response body.");
        return body;
    }

    public async Task<CancelBookingResponse> CancelBookingByArtistAsync(
        Guid bookingId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync($"api/bookings/{bookingId}/cancel-artist", cancellationToken);
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
        var resp = await PostEmptyAsync($"api/messages/{messageId}/read", cancellationToken);
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
        var resp = await PutJsonAsync("api/availability/pattern", request, cancellationToken);
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
        var resp = await PostJsonAsync("api/availability/overrides", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task RemoveMyAvailabilityOverrideAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var resp = await DeleteHttpAsync($"api/availability/overrides/{date:yyyy-MM-dd}", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public Task<LeadTimesResponse> GetMyLeadTimesAsync(CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<LeadTimesResponse>("api/availability/lead-times", cancellationToken);

    public async Task SetMyLeadTimesAsync(
        SetLeadTimesRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await PutJsonAsync("api/availability/lead-times", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<IcalFeedResponse> RotateIcalTokenAsync(CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync("api/availability/ical/rotate", cancellationToken);
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

        var resp = await SendAsync(
            HttpMethod.Post, $"api/portfolio/healed-photos/{bookingId}", form, cancellationToken);
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
        var resp = await PutJsonAsync("api/notifications/preferences", request, cancellationToken);
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
        var resp = await DeleteHttpAsync(
            $"api/notifications/push-subscriptions/{subscriptionId}", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    // ---- Admin tooling (Phase 22) ----

    public Task<IReadOnlyList<VerificationQueueItemResponse>> GetVerificationQueueAsync(
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<IReadOnlyList<VerificationQueueItemResponse>>(
            "api/admin/verification-queue", cancellationToken);

    public async Task ReviewCredentialAsync(
        string kind, Guid credentialId, ReviewCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        var resp = await PostJsonAsync(
            $"api/admin/credentials/{kind}/{credentialId}/review", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public Task<TrustSafetyDashboardResponse> GetTrustSafetyDashboardAsync(
        CancellationToken cancellationToken = default) =>
        GetAndDeserializeAsync<TrustSafetyDashboardResponse>(
            "api/admin/trust-safety", cancellationToken);

    public async Task SuspendUserAsync(
        Guid userId, SuspendUserRequest request, CancellationToken cancellationToken = default)
    {
        var resp = await PostJsonAsync(
            $"api/admin/users/{userId}/suspend", request, cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task UnsuspendUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var resp = await PostEmptyAsync(
            $"api/admin/users/{userId}/unsuspend", cancellationToken);
        await EnsureSuccessOrThrowAsync(resp, cancellationToken);
    }

    public async Task<Guid> WarnUserAsync(
        Guid userId, WarnUserRequest request, CancellationToken cancellationToken = default)
    {
        var created = await PostAndDeserializeAsync<WarnUserRequest, CreatedIdResponse>(
            $"api/admin/users/{userId}/warn", request, cancellationToken);
        return created.Id;
    }

    // ---- internals ----

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        using var msg = new HttpRequestMessage(method, path);
        if (content is not null) msg.Content = content;
        var token = await _auth.GetAccessTokenAsync(refresh: null, cancellationToken);
        if (!string.IsNullOrEmpty(token))
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await _http.SendAsync(msg, cancellationToken);
    }

    private Task<HttpResponseMessage> PostJsonAsync<T>(
        string path, T body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, JsonContent.Create(body), cancellationToken);

    private Task<HttpResponseMessage> PutJsonAsync<T>(
        string path, T body, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Put, path, JsonContent.Create(body), cancellationToken);

    private Task<HttpResponseMessage> GetHttpAsync(
        string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, path, null, cancellationToken);

    private Task<HttpResponseMessage> DeleteHttpAsync(
        string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Delete, path, null, cancellationToken);

    private Task<HttpResponseMessage> PostEmptyAsync(
        string path, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, null, cancellationToken);

    private async Task<TResponse> PostAndDeserializeAsync<TRequest, TResponse>(
        string path, TRequest request, CancellationToken cancellationToken)
    {
        var response = await PostJsonAsync(path, request, cancellationToken);
        await EnsureSuccessOrThrowAsync(response, cancellationToken);
        var body = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken)
            ?? throw new NeedlrApiException(
                (int)response.StatusCode, null, "Empty response body.");
        return body;
    }

    private async Task<TResponse> GetAndDeserializeAsync<TResponse>(
        string path, CancellationToken cancellationToken)
    {
        var response = await GetHttpAsync(path, cancellationToken);
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

        // Errors collection can be null when the API surfaces a non-envelope problem-details
        // payload (e.g., the 500 path that goes through the global ProblemDetails handler).
        // Defensive null check on .Errors so we don't NRE on top of an already-failing request.
        var first = (apiError?.Errors is { Count: > 0 } errs) ? errs[0] : null;
        var message = first?.Message
            ?? $"Request failed with status {(int)response.StatusCode} {response.ReasonPhrase}.";
        throw new NeedlrApiException((int)response.StatusCode, first?.Code, message);
    }
}

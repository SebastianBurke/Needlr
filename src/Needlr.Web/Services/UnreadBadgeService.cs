using Needlr.Contracts.Client;

namespace Needlr.Web.Services;

/// <summary>
/// Polls <c>GET /api/messages/unread-count</c> on a long interval (60s) so the layout's
/// unread badge stays roughly fresh without hammering the API. Suspended on signed-out
/// state. v1 polls; SignalR or push-driven updates are out-of-scope per
/// <c>docs/FEATURE_SPECS.md</c> § Channel.
/// </summary>
public sealed class UnreadBadgeService : IAsyncDisposable
{
    private const int PollSeconds = 60;

    private readonly INeedlrApi _api;
    private readonly AuthState _auth;
    private CancellationTokenSource? _cts;

    public UnreadBadgeService(INeedlrApi api, AuthState auth)
    {
        _api = api;
        _auth = auth;
        _auth.Changed += OnAuthChanged;
    }

    public int Count { get; private set; }
    public event Action? Changed;

    public void Start()
    {
        if (_cts is not null || !_auth.IsAuthenticated) return;
        _cts = new CancellationTokenSource();
        _ = LoopAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        Count = 0;
        Changed?.Invoke();
    }

    private void OnAuthChanged()
    {
        if (_auth.IsAuthenticated) Start();
        else Stop();
    }

    private async Task LoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var c = await _api.GetUnreadMessageCountAsync(cancellationToken);
                if (c != Count)
                {
                    Count = c;
                    Changed?.Invoke();
                }
            }
            catch { /* network blip / 401 / etc. — keep looping */ }

            try { await Task.Delay(TimeSpan.FromSeconds(PollSeconds), cancellationToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    public ValueTask DisposeAsync()
    {
        _auth.Changed -= OnAuthChanged;
        Stop();
        return ValueTask.CompletedTask;
    }
}

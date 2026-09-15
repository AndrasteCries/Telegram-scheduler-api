using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TdLib;
using TelegramScheduler.Data;

namespace TelegramScheduler.Services;

public class TelegramSessionManager
{
    private readonly ConcurrentDictionary<long, TelegramUserSession> _pendingSessions = new();
    private readonly ConcurrentDictionary<long, TelegramUserSession> _accountSessions = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _accountLocks = new();

    private readonly TdLibOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;

    public TelegramSessionManager(IOptions<TdLibOptions> options, IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
    }

    public TelegramUserSession GetOrCreatePending(long userId)
    {
        return _pendingSessions.GetOrAdd(userId, id =>
        {
            var sessionPath = Path.Combine(_options.SessionsRoot, "pending", $"{id}_{Guid.NewGuid():N}");
            var client = new TdClient();
            var session = new TelegramUserSession { Client = client, SessionPath = sessionPath };

            client.UpdateReceived += (sender, update) =>
                OnUpdateReceived(id, session, update);

            return session;
        });
    }


    public bool TryGetPending(long userId, out TelegramUserSession session)
        => _pendingSessions.TryGetValue(userId, out session!);

    public void PromoteToAccount(long userId, long telegramAccountId)
    {
        if (_pendingSessions.TryRemove(userId, out var session))
            _accountSessions[telegramAccountId] = session;
    }


    public bool TryGetByAccount(long telegramAccountId, out TelegramUserSession session)
        => _accountSessions.TryGetValue(telegramAccountId, out session!);

    public async Task ResetPendingAsync(long userId)
    {
        if (_pendingSessions.TryRemove(userId, out var session))
        {
            try
            {
                await session.Client.ExecuteAsync(new TdApi.Close());
            }
            catch
            {
            }
        }

        var dir = Path.Combine(_options.SessionsRoot, "pending", userId.ToString());
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    private async void OnUpdateReceived(long key, TelegramUserSession session, TdApi.Update update)
    {
        if (update is TdApi.Update.UpdateAuthorizationState authUpdate1)
            Console.WriteLine($"[session {key}] auth state: {authUpdate1.AuthorizationState.GetType().Name}");

        if (update is not TdApi.Update.UpdateAuthorizationState authUpdate)
            return;

        switch (authUpdate.AuthorizationState)
        {
            case TdApi.AuthorizationState.AuthorizationStateWaitTdlibParameters:
                await session.Client.SetTdlibParametersAsync(
                    apiId: _options.ApiId,
                    apiHash: _options.ApiHash,
                    databaseDirectory: session.SessionPath,
                    filesDirectory: Path.Combine(session.SessionPath, "files"),
                    systemLanguageCode: "en",
                    deviceModel: "Server",
                    applicationVersion: "1.0"
                );
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber:
                UpdateState(session, TelegramAuthState.WaitPhoneNumber);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitCode:
                UpdateState(session, TelegramAuthState.WaitCode);
                break;

            case TdApi.AuthorizationState.AuthorizationStateWaitPassword:
                UpdateState(session, TelegramAuthState.WaitPassword);
                break;

            case TdApi.AuthorizationState.AuthorizationStateReady:
                UpdateState(session, TelegramAuthState.Ready);
                break;
        }
    }

    private static void UpdateState(TelegramUserSession session, TelegramAuthState state)
    {
        session.State = state;
        if (!session.StateReached.Task.IsCompleted)
            session.StateReached.TrySetResult(state);
    }

    public Task<TelegramAuthState> WaitForAsync(TelegramUserSession session, TelegramAuthState target,
        TimeSpan? timeout = null)
        => WaitForAnyAsync(session, new[] { target }, timeout);


    public async Task<TelegramAuthState> WaitForAnyAsync(TelegramUserSession session, TelegramAuthState[] targets,
        TimeSpan? timeout = null)
    {
        if (targets.Contains(session.State))
            return session.State;

        var tcs = new TaskCompletionSource<TelegramAuthState>();

        void Handler(object? sender, TdApi.Update update)
        {
            if (update is not TdApi.Update.UpdateAuthorizationState authUpdate) return;
            var mapped = MapState(authUpdate.AuthorizationState);
            if (targets.Contains(mapped)) tcs.TrySetResult(mapped);
        }

        session.Client.UpdateReceived += Handler;

        try
        {
            var delay = Task.Delay(timeout ?? TimeSpan.FromSeconds(30));
            var completed = await Task.WhenAny(tcs.Task, delay);
            return completed == tcs.Task ? tcs.Task.Result : TelegramAuthState.Error;
        }
        finally
        {
            session.Client.UpdateReceived -= Handler;
        }
    }

    private static TelegramAuthState MapState(TdApi.AuthorizationState state) => state switch
    {
        TdApi.AuthorizationState.AuthorizationStateWaitPhoneNumber => TelegramAuthState.WaitPhoneNumber,
        TdApi.AuthorizationState.AuthorizationStateWaitCode => TelegramAuthState.WaitCode,
        TdApi.AuthorizationState.AuthorizationStateWaitPassword => TelegramAuthState.WaitPassword,
        TdApi.AuthorizationState.AuthorizationStateReady => TelegramAuthState.Ready,
        TdApi.AuthorizationState.AuthorizationStateClosed => TelegramAuthState.Error,
        _ => TelegramAuthState.None
    };

    public async Task<TelegramUserSession> GetOrRestoreAccountSessionAsync(long telegramAccountId)
    {
        if (_accountSessions.TryGetValue(telegramAccountId, out var existing))
            return existing;

        var gate = _accountLocks.GetOrAdd(telegramAccountId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();

        try
        {
            if (_accountSessions.TryGetValue(telegramAccountId, out existing))
                return existing;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var account = await db.TelegramAccounts.FindAsync(telegramAccountId);
            if (account == null)
                throw new InvalidOperationException($"TelegramAccount {telegramAccountId} not found");

            var client = new TdClient();
            var session = new TelegramUserSession
            {
                Client = client,
                SessionPath = account.TdlibSessionPath
            };

            client.UpdateReceived += (sender, update) =>
                OnUpdateReceived(telegramAccountId, session, update);

            _accountSessions[telegramAccountId] = session;

            var state = await WaitForAnyAsync(session, new[]
            {
                TelegramAuthState.Ready,
                TelegramAuthState.Error
            }, TimeSpan.FromSeconds(15));

            if (state != TelegramAuthState.Ready)
            {
                _accountSessions.TryRemove(telegramAccountId, out _);
                throw new InvalidOperationException($"Failed to restore session for account {telegramAccountId}, ended in {state}");
            }
            await Task.Delay(1000);

            return session;
        }
        finally
        {
            gate.Release();
        }
    }
}
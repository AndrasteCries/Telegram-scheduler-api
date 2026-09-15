using TdLib;
using TelegramScheduler.Models;

namespace TelegramScheduler.Services;

public class TelegramSessionService
{
    private readonly TelegramSessionManager _sessionManager;

    public TelegramSessionService(
        TelegramSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public async Task RequestCodeAsync(long userId, string phoneNumber)
    {
        await _sessionManager.ResetPendingAsync(userId);
        var session = _sessionManager.GetOrCreatePending(userId);

        await _sessionManager.WaitForAsync(session, TelegramAuthState.WaitPhoneNumber);
        await session.Client.SetAuthenticationPhoneNumberAsync(phoneNumber);
    }

    public async Task<TelegramAuthState> ConfirmCodeAsync(long userId, string code)
    {
        if (!_sessionManager.TryGetPending(userId, out var session))
            throw new InvalidOperationException("Session not found");

        await session.Client.CheckAuthenticationCodeAsync(code);

        return await _sessionManager.WaitForAnyAsync(session, new[]
        {
            TelegramAuthState.Ready,
            TelegramAuthState.WaitPassword,
            TelegramAuthState.Error
        });
    }
}
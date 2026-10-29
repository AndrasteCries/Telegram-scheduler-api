using TdLib;
using TelegramScheduler.Models;
using TelegramScheduler.Services.Exeptions;

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
    
    public async Task<TelegramAuthState> RequestReauthenticationCodeAsync(long telegramAccountId, string sessionPath, string phoneNumber)
    {
        var session = await _sessionManager.GetOrCreateReauthenticationSessionAsync(telegramAccountId, sessionPath);

        if (session.State == TelegramAuthState.Ready)
        {
            throw new ValidationException(
                "Telegram account is already authorized.");
        }

        if (session.State != TelegramAuthState.WaitPhoneNumber)
        {
            throw new ValidationException(
                $"Unexpected Telegram authorization state: {session.State}");
        }

        await session.Client.SetAuthenticationPhoneNumberAsync(phoneNumber);

        return await _sessionManager.WaitForAnyAsync(session, new[]
            {
                TelegramAuthState.WaitCode,
                TelegramAuthState.WaitPassword,
                TelegramAuthState.Ready,
                TelegramAuthState.Error
            }, TimeSpan.FromSeconds(60));
    }
    
    public async Task<TelegramAuthState> ConfirmReauthenticationCodeAsync(long telegramAccountId, string code)
    {
        if (!_sessionManager.TryGetByAccount(telegramAccountId, out var session))
        {
            throw new InvalidOperationException(
                "Reauthentication session not found.");
        }

        await session.Client.CheckAuthenticationCodeAsync(code);

        return await _sessionManager.WaitForAnyAsync(session, new[]
            {
                TelegramAuthState.Ready,
                TelegramAuthState.WaitPassword,
                TelegramAuthState.Error
            }, TimeSpan.FromSeconds(60));
    }
}
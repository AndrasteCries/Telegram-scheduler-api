using TdLib;

namespace TelegramScheduler.Services;

public enum TelegramAuthState
{
    None,
    WaitPhoneNumber,
    WaitCode,
    WaitPassword,
    Ready,
    Error
}

public class TelegramUserSession
{
    public required TdClient Client { get; init; }
    public TelegramAuthState State { get; set; } = TelegramAuthState.None;
    public string? SessionPath { get; set; }
    public string? Error { get; set; }
    public TaskCompletionSource<TelegramAuthState> StateReached { get; } = new();
}
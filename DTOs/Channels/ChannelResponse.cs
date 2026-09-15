namespace TelegramScheduler.DTOs.Channels;

public record ChannelResponse(
    long Id,
    long TelegramChatId,
    string Title,
    string? Username,
    bool IsEnabled,
    string? AvatarUrl);
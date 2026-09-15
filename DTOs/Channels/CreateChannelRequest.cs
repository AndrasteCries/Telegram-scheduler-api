namespace TelegramScheduler.DTOs.Channels;

public class CreateChannelRequest
{
    public long UserId { get; set; }
    public long TelegramChatId { get; set; }

    public string Title { get; set; } = null!;

    public string? Username { get; set; }

    public bool IsEnabled { get; set; } = true; 
}
namespace TelegramScheduler.Models;

public class Channel : Entity
{
    public long TelegramAccountId { get; set; }

    public TelegramAccount TelegramAccount { get; set; } = null!;

    public long TelegramChatId { get; set; }

    public string Title { get; set; } = null!;

    public string? Username { get; set; }

    public bool IsEnabled { get; set; } = true;

    public ICollection<Post> Posts { get; set; } = [];

    public ICollection<SlotTemplate> SlotTemplates { get; set; } = [];
    public string? PhotoPath { get; set; }  
}
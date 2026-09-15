namespace TelegramScheduler.Models;

public class Post : Entity
{
    public long ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;

    public string? Text { get; set; } = "";

    public DateTime ScheduledAtUtc { get; set; }

    public DateTime? SentAtUtc { get; set; }

    public PostStatus Status { get; set; }

    public string? HangfireJobId { get; set; }

    public ICollection<PostAttachment> Attachments { get; set; } = [];
}
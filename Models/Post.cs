namespace TelegramScheduler.Models;

public class Post
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime ScheduledAtUtc { get; set; }
    public string? HangfireJobId { get; set; }
    public bool IsSent { get; set; } = false;   
}
namespace TelegramScheduler.Models;

public enum PostStatus
{
    Draft = 0,
    Scheduled = 1,
    Sending = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5
}
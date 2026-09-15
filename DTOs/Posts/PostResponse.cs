using TelegramScheduler.Models;

namespace TelegramScheduler.DTOs.Posts;


public record PostResponse(
    long Id,
    long ChannelId,
    string Text,
    DateTime ScheduledAtUtc,
    PostStatus Status,
    List<AttachmentResponse> Attachments);
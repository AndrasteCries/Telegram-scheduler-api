namespace TelegramScheduler.DTOs.Posts;

public record AttachmentResponse(
    long Id,
    string FileName,
    string MimeType,
    long Size,
    int Order,
    string Url);
namespace TelegramScheduler.Models;

public class PostAttachment : Entity
{
    public long PostId { get; set; }
    
    public Post Post { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long Size { get; set; }

    public StorageProvider StorageProvider { get; set; }

    public string StorageKey { get; set; } = null!;

    public string? TelegramFileId { get; set; }

    public int Order { get; set; }
}
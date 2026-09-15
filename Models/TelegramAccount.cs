namespace TelegramScheduler.Models;

public class TelegramAccount : Entity
{
    public long UserId { get; set; }

    public User User { get; set; } = null!;
    
    public long TelegramUserId { get; set; }

    public string? Username { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string TdlibSessionPath { get; set; } = null!;

    public ICollection<Channel> Channels { get; set; } = [];
    
    public string PhotoPath { get; set; }
}
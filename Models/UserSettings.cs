using TelegramScheduler.Models;

public class UserSettings
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = null!;

    public string Theme { get; set; } = "dark";
    public string AccentColor { get; set; } = "#3b82f6";
    public string Language { get; set; } = "en";
    public string TimeZone { get; set; } = "Europe/Kyiv";
}
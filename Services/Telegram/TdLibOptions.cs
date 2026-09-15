namespace TelegramScheduler.Services;

public class TdLibOptions
{
    public int ApiId { get; set; }
    public string ApiHash { get; set; } = "";
    public string SessionsRoot { get; set; } = "tdlib-sessions";
}
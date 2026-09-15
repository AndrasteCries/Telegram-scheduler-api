namespace TelegramScheduler.Models;

public class User : Entity
{
    public string Login { get; set; } = null!;
    public string NormalizedLogin { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public UserSettings Settings { get; set; } = null!;

    public ICollection<TelegramAccount> TelegramAccounts { get; set; } = [];
}

using TelegramScheduler.Models;

namespace TelegramScheduler.DTOs.User;

public class UserResponse
{
    public long Id { get; set; }
    public string Login { get; set; } = null!;
}
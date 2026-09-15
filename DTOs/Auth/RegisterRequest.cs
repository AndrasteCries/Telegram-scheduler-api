using System.ComponentModel.DataAnnotations;

namespace TelegramScheduler.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    [StringLength(64, MinimumLength = 3)]
    public string Login { get; set; } = string.Empty;
    
    [Required]
    [StringLength(128, MinimumLength = 3)]
    public string Password { get; set; } = string.Empty;
}
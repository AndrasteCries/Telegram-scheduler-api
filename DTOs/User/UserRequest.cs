using System.ComponentModel.DataAnnotations;

namespace TelegramScheduler.DTOs.User;

public class UserRequest
{
    [Required] public long UserId { get; set; }
}
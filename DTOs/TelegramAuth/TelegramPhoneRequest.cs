using System.ComponentModel.DataAnnotations;

namespace TelegramScheduler.DTOs.TelegramAuth;

public class TelegramPhoneRequest
{
    [Required]
    public string PhoneNumber { get; set; }
}
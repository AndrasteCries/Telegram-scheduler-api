using System.ComponentModel.DataAnnotations;

namespace TelegramScheduler.DTOs.TelegramAuth;

public class TelegramCodeRequest
{
    [Required]
    public string Code { get; set; }
}
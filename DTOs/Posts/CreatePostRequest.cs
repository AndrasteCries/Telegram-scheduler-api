using Microsoft.AspNetCore.Mvc;

namespace TelegramScheduler.DTOs.Posts;

public class CreatePostRequest
{
    [FromForm(Name = "text")]
    public string Text { get; set; } = "";

    [FromForm(Name = "scheduledAtUtc")] public string ScheduledAtUtc { get; set; } = ""; 

    public List<IFormFile> Files { get; set; } = [];
}
using Microsoft.AspNetCore.Mvc;

namespace TelegramScheduler.DTOs.Posts;

public class UpdatePostRequest
{
        [FromForm(Name = "text")]
        public string? Text { get; set; }

        [FromForm(Name = "scheduledAtUtc")]
        public string? ScheduledAtUtc { get; set; }

        [FromForm(Name = "keepAttachmentIds")]
        public List<long> KeepAttachmentIds { get; set; } = [];

        public List<IFormFile> NewFiles { get; set; } = [];
    }
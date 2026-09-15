namespace TelegramScheduler.DTOs.Posts;

public class SlotTemplateRequest
{
    public long Id { get; set; }
    
    public TimeOnly TimeOfDay { get; set; }

    public string Label { get; set; } = null!;
}
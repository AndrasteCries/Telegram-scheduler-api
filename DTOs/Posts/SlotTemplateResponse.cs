namespace TelegramScheduler.DTOs.Posts;

public record SlotTemplateResponse(
    long Id,
    long ChannelId,
    TimeOnly TimeOfDay,
    string Label,
    int SortOrder);
namespace TelegramScheduler.DTOs.Posts;


public record UpsertSlotTemplateRequest(
    long? Id,
    TimeOnly TimeOfDay,
    string Label,
    int SortOrder);
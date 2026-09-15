using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Models;

namespace TelegramScheduler.Mappings;

public static class SlotTemplateMappings
{
    public static SlotTemplateResponse ToResponse(this SlotTemplate template)
    {
        return new SlotTemplateResponse(
            template.Id,
            template.ChannelId,
            template.TimeOfDay,
            template.Label,
            template.SortOrder
        );
    }
}


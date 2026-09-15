namespace TelegramScheduler.Models;

public class SlotTemplate : Entity
{
    public long ChannelId { get; set; }
    public Channel Channel { get; set; } = null!;

    public TimeOnly TimeOfDay { get; set; }

    public string Label { get; set; } = null!;

    public int SortOrder { get; set; }
}
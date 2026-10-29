using System.Globalization;
using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services.Posts;

public class PostDateValidator
{
    public DateTime ParseAndValidate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ValidationException(
                "ScheduledAtUtc is required.");
        }

        if (!DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal |
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            throw new ValidationException(
                "Invalid scheduledAtUtc format.");
        }

        if (date <= DateTime.UtcNow)
        {
            throw new ValidationException(
                "Scheduled time must be in the future.");
        }

        return date;
    }
}
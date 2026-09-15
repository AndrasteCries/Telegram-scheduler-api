using TelegramScheduler.DTOs.Channels;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Models;
using TelegramScheduler.Services;

namespace TelegramScheduler.Mappings;

public static class PostMappings
{
    public static PostResponse ToResponse(this Post post, SignedUrlService signedUrlService)
    {
        return new PostResponse(
            post.Id,
            post.ChannelId,
            post.Text,
            post.ScheduledAtUtc,
            post.Status,
            post.Attachments.Select(a => a.ToResponse(signedUrlService)).ToList()
        );
    }
}
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Models;

namespace TelegramScheduler.Mappings;

public static class CreatePostRequestMappings
{
    public static Post ToPost(this CreatePostRequest request, long channelId, DateTime timeToPost)
    {
        var post = new Post
        {
            ChannelId = channelId,
            Text = request.Text,
            ScheduledAtUtc = timeToPost,
            Status = PostStatus.Scheduled
        };
        
        return post;
    }
}
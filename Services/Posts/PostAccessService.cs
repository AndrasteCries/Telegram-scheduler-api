using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.Models;
using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services.Posts;

public class PostAccessService
{
    private readonly AppDbContext _db;

    public async Task<Channel> GetOwnedChannelAsync(long channelId, long userId)
    {
        var channel = await _db.Channels
            .Include(x => x.TelegramAccount)
            .FirstOrDefaultAsync(x =>
                x.Id == channelId &&
                x.TelegramAccount.UserId == userId);

        if (channel == null)
        {
            throw new NotFoundException(
                "Channel not found.");
        }

        return channel;
    }

    public async Task<Post> GetOwnedPostAsync(long postId, long channelId, long userId)
    {
        var post = await _db.Posts
            .Include(x => x.Attachments)
            .Include(x => x.Channel)
            .ThenInclude(x => x.TelegramAccount)
            .FirstOrDefaultAsync(x =>
                x.Id == postId &&
                x.ChannelId == channelId &&
                x.Channel.TelegramAccount.UserId == userId);

        if (post == null)
        {
            throw new NotFoundException(
                "Post not found.");
        }

        return post;
    }
}
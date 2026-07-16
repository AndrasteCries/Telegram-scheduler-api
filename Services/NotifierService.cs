using Microsoft.AspNetCore.Http.HttpResults;
using TelegramScheduler.Data;
using Telegram.Bot;

namespace TelegramScheduler.Services;

public class NotifierService(AppDbContext db, ITelegramBotClient bot)
{
    public async Task NotifyAsync(long chatId, string text)
    {
        await bot.SendMessage(chatId, text);
    }

    public async Task NotifyByPostAsync(int postId)
    {
        var post = await db.Posts.FindAsync(postId);
        if (post != null)
            await bot.SendMessage(post.ChatId, post.Text);
    }
}
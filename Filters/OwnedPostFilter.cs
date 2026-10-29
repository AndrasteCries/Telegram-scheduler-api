using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.Models;

namespace TelegramScheduler.Filters;

public class OwnedPostFilter : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.Items["OwnedChannel"] is not Channel channel)
        {
            context.Result = new StatusCodeResult(500);
            return;
        }

        if (!context.ActionArguments.TryGetValue("postId", out var postIdObj) || postIdObj is not long postId)
        {
            context.Result = new BadRequestObjectResult("postId route parameter is required");
            return;
        }

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var post = await db.Posts
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == postId && p.ChannelId == channel.Id);

        if (post == null)
        {
            context.Result = new NotFoundObjectResult("Post not found");
            return;
        }

        context.HttpContext.Items["OwnedPost"] = post;

        await next();
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;

namespace TelegramScheduler.Filters;

public class OwnedChannelFilter : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        if (!context.ActionArguments.TryGetValue("channelId", out var channelIdObj) || channelIdObj is not long channelId)
        {
            context.Result = new BadRequestObjectResult("channelId route parameter is required");
            return;
        }

        var userIdClaim = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userId = long.Parse(userIdClaim!);

        var channel = await db.Channels
            .Include(c => c.TelegramAccount)
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TelegramAccount.UserId == userId);

        if (channel == null)
        {
            context.Result = new NotFoundObjectResult("Channel not found");
            return;
        }

        context.HttpContext.Items["OwnedChannel"] = channel;

        await next();
    }
}
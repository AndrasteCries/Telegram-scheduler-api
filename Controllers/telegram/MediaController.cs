using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;

namespace TelegramScheduler.Controllers.telegram;

[ApiController]
[Route("api/media")]
[Authorize]
public class MediaController : ControllerBase
{
    private readonly AppDbContext _db;

    public MediaController(AppDbContext db)
    {
        _db = db;
    }

    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [AllowAnonymous]
    [HttpGet("channels/{channelId}/avatar")]
    public async Task<IActionResult> GetChannelAvatar(long channelId)
    {
        var channel = await _db.Channels
            .Include(c => c.TelegramAccount)
            .FirstOrDefaultAsync(c => c.Id == channelId);

        if (channel == null || channel.TelegramAccount.UserId != CurrentUserId)
            return NotFound();

        if (channel.PhotoPath == null || !System.IO.File.Exists(channel.PhotoPath))
            return NotFound();

        return PhysicalFile(Path.GetFullPath(channel.PhotoPath), "image/jpeg");
    }

    [AllowAnonymous]
    [HttpGet("accounts/{accountId}/avatar")]
    public async Task<IActionResult> GetAccountAvatar(long accountId)
    {
        var account = await _db.TelegramAccounts.FindAsync(accountId);

        if (account == null || account.UserId != CurrentUserId)
            return NotFound();

        if (account.PhotoPath == null || !System.IO.File.Exists(account.PhotoPath))
            return NotFound();

        return PhysicalFile(Path.GetFullPath(account.PhotoPath), "image/jpeg");
    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Channels;
using TelegramScheduler.DTOs.TelegramAuth;
using TelegramScheduler.Models;
using TelegramScheduler.Services;

namespace TelegramScheduler.Controllers.telegram;

[Authorize]
[ApiController]
[Route("api/telegram/session")]
public class TelegramSessionController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TelegramSessionService _telegramSessionService;
    private readonly TelegramChannelService _channelService;
    private readonly TelegramAccountService _accountService;

    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public TelegramSessionController(AppDbContext db, TelegramSessionService telegramSessionService,
        TelegramChannelService channelService, TelegramAccountService accountService)
    {
        _db = db;
        _telegramSessionService = telegramSessionService;
        _channelService = channelService;
        _accountService = accountService;
    }

    [HttpPost("phone")]
    public async Task<ActionResult<TelegramPhoneResponse>> SendCode(TelegramPhoneRequest request)
    {
        var user = await _db.Users.FindAsync(CurrentUserId);

        if (user == null)
        {
            Console.WriteLine($"[SendCode] User {CurrentUserId} not found");
            return NotFound();
        }

        try
        {
            await _telegramSessionService.RequestCodeAsync(CurrentUserId, request.PhoneNumber);
            return Ok(new TelegramPhoneResponse("code_sent"));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SendCode] Error: {ex}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("code")]
    public async Task<ActionResult> ConfirmCode(TelegramCodeRequest request)
    {
        var userId = CurrentUserId;
        var user = await _db.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        try
        {
            var success = await _telegramSessionService.ConfirmCodeAsync(CurrentUserId, request.Code);

            if (success != TelegramAuthState.Ready)
            {
                Console.WriteLine($"[ConfirmCode] User {CurrentUserId} ended in state: {success}");
                return BadRequest(new { status = success.ToString() });
            }

            var account = await _accountService.AddNewAccountToUserAsync(user.Id);

            return Ok(new { status = "ready", telegramAccountId = account.Id });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ConfirmCode] Error for user {CurrentUserId}: {ex}");
            return BadRequest(new { status = "invalid_code", error = ex.Message });
        }
    }

    [HttpGet("get-users")]
    public async Task<ActionResult<List<TelegramAccountResponse>>> GetTelegramUsers()
    {
        var accounts = await _accountService.GetAllTelegramAccountsAsync(CurrentUserId);
        return Ok(accounts);
    }

    [HttpPost("accounts/{telegramAccountId}/reauth/phone")]
    public async Task<ActionResult<TelegramPhoneResponse>> StartReauthentication(long telegramAccountId, TelegramPhoneRequest request)
    {
        var state = await _accountService.StartReauthenticationAsync(telegramAccountId, CurrentUserId, request.PhoneNumber);

        return Ok(new TelegramPhoneResponse(state.ToString()));
    }
    
    [HttpPost("accounts/{telegramAccountId}/reauth/code")]
    public async Task<ActionResult<TelegramAuthResult>> ConfirmReauthentication(long telegramAccountId, TelegramCodeRequest request)
    {
        var state = await _accountService.ConfirmReauthenticationAsync(telegramAccountId, CurrentUserId, request.Code);

        return Ok(new TelegramAuthResult
        {
            Status = state.ToString()
        });
    }

    [HttpGet("channels/sync")]
    public async Task<ActionResult<List<ChannelResponse>>> SyncPostableChannels([FromQuery] long telegramAccountId)
    {
        var account = await _db.TelegramAccounts
            .FirstOrDefaultAsync(a =>
                a.Id == telegramAccountId &&
                a.UserId == CurrentUserId);

        if (account == null) return NotFound();

        await _channelService.SyncPostableChannelsAsync(telegramAccountId);
        var channels = await _channelService.GetChannelsAsync(telegramAccountId);
        return Ok(channels);
    }
}
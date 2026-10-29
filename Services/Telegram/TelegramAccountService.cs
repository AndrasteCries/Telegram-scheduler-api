using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TdLib;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.TelegramAuth;
using TelegramScheduler.Models;
using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services;

public class TelegramAccountService
{
    private readonly AppDbContext _db;
    private readonly TelegramSessionManager _sessionManager;
    private readonly SignedUrlService _signedUrlService;
    private readonly TelegramSessionService _sessionService;

    public TelegramAccountService(
        AppDbContext db,
        TelegramSessionManager sessionManager,
        SignedUrlService signedUrlService,
        TelegramSessionService sessionService)
    {
        _db = db;
        _sessionManager = sessionManager;
        _signedUrlService = signedUrlService;
        _sessionService = sessionService;
    }

    public async Task<TelegramAccount> AddNewAccountToUserAsync(long userId)
    {
        if (!_sessionManager.TryGetPending(userId, out var session))
            throw new InvalidOperationException("Session not found");

        var me = await session.Client.GetMeAsync();

        var existing = await _db.TelegramAccounts
            .FirstOrDefaultAsync(a => a.TelegramUserId == me.Id);

        if (existing != null)
        {
            _sessionManager.PromoteToAccount(userId, existing.Id);
            return existing;
        }

        var account = new TelegramAccount
        {
            UserId = userId,
            TelegramUserId = me.Id,
            Username = me.FirstName + ' ' +  me.LastName,
            PhoneNumber = me.PhoneNumber,
            TdlibSessionPath = session.SessionPath 
        };

        _db.TelegramAccounts.Add(account);
        await _db.SaveChangesAsync();

        _sessionManager.PromoteToAccount(userId, account.Id); 

        return account;
    }

    public async Task<List<TelegramAccountResponse>> GetAllTelegramAccountsAsync(long userId)
    {
        var accounts = await _db.TelegramAccounts.Where(a => a.UserId == userId).ToListAsync();
        return accounts.Select(a => new TelegramAccountResponse(
            a.Id,
            a.Username,
            a.PhoneNumber,
            a.PhotoPath != null
                ? _signedUrlService.GenerateSignedUrl($"/api/media/accounts/{a.Id}/avatar", TimeSpan.FromHours(1))
                : null
        )).ToList();
    }

    public async Task<bool> CheckTelegramAccountAuthAsync(long telegramUserId)
    { 
        if (!_sessionManager.TryGetPending(telegramUserId, out var session))
            throw new InvalidOperationException("Session not found");
        
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.UserId == telegramUserId);
        
        return account?.Status == TelegramAccountStatus.NeedsReauthentication;
    }
    
    public async Task<TelegramAuthState> StartReauthenticationAsync(long telegramAccountId, long userId, string phoneNumber)
    {
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(x => x.Id == telegramAccountId && x.UserId == userId);

        if (account == null)
        {
            throw new NotFoundException("Telegram account not found.");
        }

        account.Status = TelegramAccountStatus.NeedsReauthentication;

        await _db.SaveChangesAsync();

        var state = await _sessionService.RequestReauthenticationCodeAsync(account.Id, account.TdlibSessionPath, phoneNumber);

        if (state == TelegramAuthState.Ready)
        {
            account.Status = TelegramAccountStatus.Connected;

            await _db.SaveChangesAsync();
        }

        return state;
    }
    
    public async Task<TelegramAuthState> ConfirmReauthenticationAsync(long telegramAccountId, long userId, string code)
    {
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(x => x.Id == telegramAccountId && x.UserId == userId);

        if (account == null)
        {
            throw new NotFoundException("Telegram account not found.");
        }

        var state = await _sessionService.ConfirmReauthenticationCodeAsync(account.Id, code);

        if (state == TelegramAuthState.Ready)
        {
            account.Status = TelegramAccountStatus.Connected;

            await UpdateTelegramAccountInfoAsync(account);

            await _db.SaveChangesAsync();
        }

        return state;
    }
    
    private async Task UpdateTelegramAccountInfoAsync(TelegramAccount account)
    {
        if (!_sessionManager.TryGetByAccount(account.Id, out var session))
        {
            return;
        }

        var me = await session.Client.GetMeAsync();

        account.TelegramUserId = me.Id;
        account.Username = $"{me.FirstName} {me.LastName}".Trim();
        account.PhoneNumber = me.PhoneNumber;
    }
}
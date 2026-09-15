using Microsoft.EntityFrameworkCore;
using TdLib;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.TelegramAuth;
using TelegramScheduler.Models;

namespace TelegramScheduler.Services;

public class TelegramAccountService
{
    private readonly AppDbContext _db;
    private readonly TelegramSessionManager _sessionManager;
    private readonly SignedUrlService _signedUrlService;

    public TelegramAccountService(AppDbContext db, TelegramSessionManager sessionManager, SignedUrlService signedUrlService)
    {
        _db = db;
        _sessionManager = sessionManager;
        _signedUrlService = signedUrlService;
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

        _sessionManager.PromoteToAccount(userId, account.Id); // <-- ключевой момент

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
}
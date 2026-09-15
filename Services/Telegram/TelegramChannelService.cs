using Microsoft.EntityFrameworkCore;
using TdLib;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Channels;
using TelegramScheduler.Models;
using Microsoft.Extensions.Options;

namespace TelegramScheduler.Services;

public class TelegramChannelService
{
    private readonly TelegramSessionManager _sessionManager;
    private readonly AppDbContext _db;
    private readonly TelegramMediaService _mediaService;
    private readonly MediaStorageOptions _mediaOptions;
    private readonly SignedUrlService _signedUrlService;

    public TelegramChannelService(
        TelegramSessionManager sessionManager,
        AppDbContext db,
        TelegramMediaService mediaService,
        IOptions<MediaStorageOptions> mediaOptions, SignedUrlService signedUrlService)
    {
        _sessionManager = sessionManager;
        _db = db;
        _mediaService = mediaService;
        _signedUrlService = signedUrlService;
        _mediaOptions = mediaOptions.Value;
    }

    public async Task<List<Channel>> SyncPostableChannelsAsync(long telegramAccountId)
    {
        var session = await _sessionManager.GetOrRestoreAccountSessionAsync(telegramAccountId);

        var fresh = await FetchPostableChannelsAsync(session.Client, telegramAccountId);

        var existing = await _db.Channels
            .Where(c => c.TelegramAccountId == telegramAccountId)
            .ToListAsync();

        foreach (var ch in fresh)
        {
            var match = existing.FirstOrDefault(c => c.TelegramChatId == ch.TelegramChatId);
            if (match != null)
            {
                match.Title = ch.Title;
                match.Username = ch.Username;
                match.IsEnabled = true;
            }
            else
            {
                _db.Channels.Add(ch);
            }
        }

        var freshIds = fresh.Select(c => c.TelegramChatId).ToHashSet();
        foreach (var stale in existing.Where(c => !freshIds.Contains(c.TelegramChatId)))
            stale.IsEnabled = false;

        await _db.SaveChangesAsync();

        return await _db.Channels
            .Where(c => c.TelegramAccountId == telegramAccountId)
            .ToListAsync();
    }

    private async Task<List<Channel>> FetchPostableChannelsAsync(TdClient client, long telegramAccountId)
    {
        while (true)
        {
            try
            {
                await client.LoadChatsAsync(new TdApi.ChatList.ChatListMain(), limit: 100);
            }
            catch (TdException)
            {
                break;
            }
        }

        var chatIds = await client.GetChatsAsync(new TdApi.ChatList.ChatListMain(), limit: 200);
        Console.WriteLine($"[FetchChannels] Got {chatIds.ChatIds.Length} chat ids");

        var result = new List<Channel>();

        foreach (var chatId in chatIds.ChatIds)
        {
            TdApi.Chat chat;
            try
            {
                chat = await client.GetChatAsync(chatId);
            }
            catch (TdException ex)
            {
                Console.WriteLine($"[FetchChannels] Skipping chat {chatId}: {ex.Message}");
                continue;
            }

            if (chat.Type is not TdApi.ChatType.ChatTypeSupergroup { IsChannel: true } supergroupType)
                continue;

            TdApi.Supergroup supergroup;
            try
            {
                supergroup = await client.GetSupergroupAsync(supergroupType.SupergroupId);
            }
            catch (TdException ex)
            {
                Console.WriteLine($"[FetchChannels] Skipping supergroup {supergroupType.SupergroupId}: {ex.Message}");
                continue;
            }

            var canPost = supergroup.Status switch
            {
                TdApi.ChatMemberStatus.ChatMemberStatusCreator => true,
                TdApi.ChatMemberStatus.ChatMemberStatusAdministrator admin => admin.Rights.CanPostMessages,
                _ => false
            };

            if (!canPost) continue;

            var photoPath = await _mediaService.DownloadPhotoAsync(
                client,
                chat.Photo?.Small,
                _mediaOptions.ChannelAvatarsPath,
                chatId.ToString()
            );

            result.Add(new Channel
            {
                TelegramAccountId = telegramAccountId,
                TelegramChatId = chatId,
                Title = chat.Title,
                Username = supergroup.Usernames?.ActiveUsernames.FirstOrDefault(),
                PhotoPath = photoPath,
                IsEnabled = true
            });
        }

        return result;
    }

    public async Task<List<ChannelResponse>> GetChannelsAsync(long telegramAccountId)
    {
        var channels = await _db.Channels
            .Where(c => c.TelegramAccountId == telegramAccountId)
            .ToListAsync();

        return channels.Select(ToResponse).ToList();
    }
    
    private ChannelResponse ToResponse(Channel c)
    {
        string? avatarUrl = null;

        if (c.PhotoPath != null)
        {
            var resourcePath = $"/api/media/channels/{c.Id}/avatar";
            avatarUrl = _signedUrlService.GenerateSignedUrl(resourcePath, TimeSpan.FromHours(1));
        }

        return new ChannelResponse(
            c.Id, c.TelegramChatId, c.Title, c.Username, c.IsEnabled, avatarUrl);
    }
}
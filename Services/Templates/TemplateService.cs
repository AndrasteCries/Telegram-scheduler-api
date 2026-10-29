using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Mappings;
using TelegramScheduler.Models;
using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services.Templates;

public class TemplateService
{
    private readonly AppDbContext _db;

    public TemplateService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<SlotTemplateResponse>> GetTemplates(long channelId, long userId) 
    {
        var channelExist = await ChannelExist(channelId, userId);
        if (!channelExist)
            throw new NotFoundException("Channel not found");
        
        var result = await _db.SlotTemplates
            .Where(t => t.ChannelId == channelId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
        
        return result.Select(t => t.ToResponse()).ToList();
    }
    
    public async Task<List<SlotTemplateResponse>> UpdateTemplates(long channelId, List<UpsertSlotTemplateRequest> request,
        long userId)
    {
        var channelExist = await ChannelExist(channelId, userId);
        if (!channelExist)
            throw new NotFoundException("Channel not found");
        
        var existing = await GetTemplatesAsync(channelId);
        
        var incomingIds = request.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToHashSet();
        foreach (var stale in existing.Where(t => !incomingIds.Contains(t.Id)))
            _db.SlotTemplates.Remove(stale);
        
        foreach (var item in request)
        {
            if (item.Id.HasValue)
            {
                var match = existing.FirstOrDefault(t => t.Id == item.Id.Value);
                if (match != null)
                {
                    match.TimeOfDay = item.TimeOfDay;
                    match.Label = item.Label;
                    match.SortOrder = item.SortOrder;
                    continue;
                }
            }

            _db.SlotTemplates.Add(new SlotTemplate
            {
                ChannelId = channelId,
                TimeOfDay = item.TimeOfDay,
                Label = item.Label,
                SortOrder = item.SortOrder
            });
        }

        await _db.SaveChangesAsync();
        
        var result = await _db.SlotTemplates
            .Where(t => t.ChannelId == channelId)
            .OrderBy(t => t.SortOrder)
            .ToListAsync();
        
        return result.Select(t => t.ToResponse()).ToList();
    }

    private async Task<List<SlotTemplate>> GetTemplatesAsync(long channelId)
    {
        var existing = await _db.SlotTemplates
            .Where(t => t.ChannelId == channelId)
            .ToListAsync();

        return existing;
    }
    
    private async Task<bool> ChannelExist(long channelId, long currentUserId)
    {
        var channel = await _db.Channels
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TelegramAccount.UserId == currentUserId);

        return channel != null;
    }
}
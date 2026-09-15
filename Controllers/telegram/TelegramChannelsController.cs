using System.Globalization;
using System.Security.Claims;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Mappings;
using TelegramScheduler.Models;
using TelegramScheduler.Services;

namespace TelegramScheduler.Controllers;

[Authorize]
[ApiController]
[Route("api/channels")]
public class TelegramChannelsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;
    private readonly MediaStorageOptions _mediaOptions;
    private readonly SignedUrlService _urlService;
    
    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public TelegramChannelsController(AppDbContext db, IBackgroundJobClient jobs, IOptions<MediaStorageOptions> mediaOptions, SignedUrlService urlService)
    {
        _db = db;
        _jobs = jobs;
        _urlService = urlService;
        _mediaOptions = mediaOptions.Value;
    }

    [HttpGet("{channelId}/posts")]
    public async Task<ActionResult<List<PostResponse>>> GetPosts(
        long channelId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        Console.WriteLine($"[GetPosts] channelId={channelId}, CurrentUserId={CurrentUserId}");

        var channel = await GetOwnedChannelAsync(channelId);

        if (channel == null)
        {
            Console.WriteLine($"[GetPosts] Channel {channelId} not found for user {CurrentUserId}");
            return NotFound("Channel not found");
        }

        if (to <= from)
            return BadRequest("'to' must be after 'from'");

        var posts = await _db.Posts
            .Include(p => p.Attachments) 
            .Where(p => p.ChannelId == channelId)
            .OrderBy(p => p.ScheduledAtUtc)
            .ToListAsync();

        return Ok(posts.Select(p => p.ToResponse(_urlService)).ToList());
    }
    
    [HttpPost("{channelId}/posts")]
    public async Task<ActionResult<PostResponse>> CreatePost(long channelId, [FromForm] CreatePostRequest request)
    {
        var channel = await _db.Channels
            .Include(c => c.SlotTemplates)
            .Include(c => c.TelegramAccount)
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TelegramAccount.UserId == CurrentUserId);

        if (channel == null)
            return NotFound("Channel not found");
        
        Console.WriteLine($"[CreatePost] Raw: '{request.ScheduledAtUtc}', Parsed: {request.ScheduledAtUtc}, Now: {DateTime.UtcNow:O}");

        if (!DateTime.TryParse(
                request.ScheduledAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var scheduledAtUtc))
        {
            return BadRequest("Invalid scheduledAtUtc format");
        }


        
        if (scheduledAtUtc  <= DateTime.UtcNow)
            return BadRequest("Scheduled time must be in the future.");

        if (request.Files.Count > 10)
            return BadRequest("Too many attachments (max 10)");
        
        foreach (var file in request.Files)
        {
            if (file.Length > 20 * 1024 * 1024)
                return BadRequest($"File {file.FileName} is too large");
            
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "video/mp4" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest($"Unsupported file type: {file.ContentType}");
        }

        var post = new Post
        {
            ChannelId = channelId,
            Text = request.Text,
            ScheduledAtUtc = scheduledAtUtc ,
            Status = PostStatus.Scheduled
        };

        var dir = Path.Combine(_mediaOptions.PostAttachmentsPath, Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);

        for (int i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i];
            var storageKey = Path.Combine(dir, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

            await using (var stream = new FileStream(storageKey, FileMode.Create))
                await file.CopyToAsync(stream);

            post.Attachments.Add(new PostAttachment
            {
                FileName = file.FileName,
                MimeType = file.ContentType,
                Size = file.Length,
                StorageProvider = StorageProvider.Local,
                StorageKey = storageKey,
                Order = i
            });
        }

        _db.Posts.Add(post);
        await _db.SaveChangesAsync();

        post.HangfireJobId = _jobs.Schedule<NotifierService>(
            n => n.NotifyByPostAsync(post.Id),
            post.ScheduledAtUtc - DateTime.UtcNow
        );

        await _db.SaveChangesAsync();

        var fullPost = await _db.Posts
            .Include(p => p.Attachments)
            .FirstAsync(p => p.Id == post.Id);

        return Ok(fullPost.ToResponse(_urlService));
    }

    [HttpPut("{channelId}/posts/{postId}")]
    public async Task<ActionResult<PostResponse>> UpdatePost(
        long channelId,
        long postId,
        UpdatePostRequest request)
    {
        var channel = await GetOwnedChannelAsync(channelId);
        if (channel == null) return NotFound("Channel not found");

        var post = await _db.Posts
            .Include(p => p.Attachments)
            .FirstOrDefaultAsync(p => p.Id == postId && p.ChannelId == channelId);

        if (post == null) return NotFound("Post not found");
        if (post.Status != PostStatus.Scheduled) return BadRequest("Only scheduled posts can be edited");

        var toRemove = post.Attachments.Where(a => !request.KeepAttachmentIds.Contains(a.Id)).ToList();
        foreach (var a in toRemove)
        {
            if (System.IO.File.Exists(a.StorageKey))
                System.IO.File.Delete(a.StorageKey);
            _db.PostAttachments.Remove(a);
        }

        var dir = Path.Combine(_mediaOptions.PostAttachmentsPath, post.Id.ToString());
        Directory.CreateDirectory(dir);

        var startOrder = request.KeepAttachmentIds.Count;
        for (int i = 0; i < request.NewFiles.Count; i++)
        {
            var file = request.NewFiles[i];
            var storageKey = Path.Combine(dir, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");

            await using (var stream = new FileStream(storageKey, FileMode.Create))
                await file.CopyToAsync(stream);

            _db.PostAttachments.Add(new PostAttachment
            {
                PostId = post.Id,
                FileName = file.FileName,
                MimeType = file.ContentType,
                Size = file.Length,
                StorageProvider = StorageProvider.Local,
                StorageKey = storageKey,
                Order = startOrder + i
            });
        }

        if (request.Text != null) post.Text = request.Text;

        if (request.ScheduledAtUtc.HasValue && request.ScheduledAtUtc != post.ScheduledAtUtc)
        {
            if (request.ScheduledAtUtc <= DateTime.UtcNow)
                return BadRequest("Scheduled time must be in the future.");

            post.ScheduledAtUtc = request.ScheduledAtUtc.Value;

            if (!string.IsNullOrEmpty(post.HangfireJobId))
                BackgroundJob.Delete(post.HangfireJobId);

            post.HangfireJobId = _jobs.Schedule<NotifierService>(
                n => n.NotifyByPostAsync(post.Id),
                post.ScheduledAtUtc - DateTime.UtcNow
            );
        }

        post.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var updated = await _db.Posts.Include(p => p.Attachments).FirstAsync(p => p.Id == post.Id);
        return Ok(updated.ToResponse(_urlService));
    }

    [HttpDelete("{channelId}/posts/{postId}")]
    public async Task<IActionResult> DeletePost(long channelId, long postId)
    {
        var channel = await GetOwnedChannelAsync(channelId);
        if (channel == null) return NotFound("Channel not found");

        var post = await _db.Posts.FirstOrDefaultAsync(p => p.Id == postId && p.ChannelId == channelId);
        if (post == null) return NotFound("Post not found");

        if (!string.IsNullOrEmpty(post.HangfireJobId))
            BackgroundJob.Delete(post.HangfireJobId);

        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{channelId}/slot-templates")]
    public async Task<ActionResult<List<SlotTemplateResponse>>> GetTemplates(long channelId)
    {
        var channel = await GetOwnedChannelAsync(channelId);
        if (channel == null) return NotFound("Channel not found");

        var templates = await _db.SlotTemplates
            .Where(t => t.ChannelId == channelId)
            .ToListAsync();

        return Ok(templates.Select(t => t.ToResponse()));
    }

    [HttpPost("{channelId}/slot-templates")]
    public async Task<ActionResult<List<SlotTemplateResponse>>> UpdateTemplates(
        long channelId,
        List<UpsertSlotTemplateRequest> request)
    {
        var channel = await GetOwnedChannelAsync(channelId);
        if (channel == null) return NotFound("Channel not found");

        var existing = await _db.SlotTemplates
            .Where(t => t.ChannelId == channelId)
            .ToListAsync();

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

        return Ok(result.Select(t => t.ToResponse()));
    }
    
    [HttpGet("~/api/attachments/{attachmentId}/file")]
    [AllowAnonymous] // проверка через подпись, не через JWT - как с аватарками
    public async Task<IActionResult> GetAttachmentFile(long attachmentId, [FromQuery] long expires, [FromQuery] string sig)
    {
        var resourcePath = $"/api/attachments/{attachmentId}/file";

        if (!_urlService.ValidateSignedUrl(resourcePath, expires, sig))
            return Unauthorized();

        var attachment = await _db.PostAttachments.FindAsync(attachmentId);
        if (attachment == null || !System.IO.File.Exists(attachment.StorageKey))
            return NotFound();

        return PhysicalFile(Path.GetFullPath(attachment.StorageKey), attachment.MimeType);
    }


    private async Task<Channel?> GetOwnedChannelAsync(long channelId)
    {
        return await _db.Channels
            .Include(c => c.TelegramAccount)
            .FirstOrDefaultAsync(c => c.Id == channelId && c.TelegramAccount.UserId == CurrentUserId);
    }
}
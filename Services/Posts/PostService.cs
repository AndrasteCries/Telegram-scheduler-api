using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TelegramScheduler.Data;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Mappings;
using TelegramScheduler.Models;
using TelegramScheduler.Services.Exeptions;

namespace TelegramScheduler.Services.Posts;

public class PostService
{
    private readonly AppDbContext _db;
    private readonly MediaStorageOptions _mediaOptions;
    private readonly IBackgroundJobClient _jobs;
    private readonly SignedUrlService _urlService;
    private readonly PostDateValidator _dateValidator;
    private readonly PostFileValidator _fileValidator;
    private readonly PostAccessService _accessService;

    public PostService(AppDbContext db, IBackgroundJobClient jobs, SignedUrlService urlService,
        IOptions<MediaStorageOptions> mediaOptions, PostFileValidator fileValidator, PostDateValidator dateValidator, PostAccessService accessService)
    {
        _db = db;
        _jobs = jobs;
        _urlService = urlService;
        _fileValidator = fileValidator;
        _dateValidator = dateValidator;
        _accessService = accessService;
        _mediaOptions = mediaOptions.Value;
    }

    public async Task<List<PostResponse>> GetPosts(long channelId, long userId, DateTime from, DateTime to)
    {
        Console.WriteLine($"[GetPosts] channelId={channelId}, CurrentUserId={userId}");
        await _accessService.GetOwnedChannelAsync(channelId, userId);

        if (to <= from)
            throw new ValidationException("'to' must be after 'from'");
        
        var posts = await _db.Posts
            .Include(p => p.Attachments)
            .Where(p => p.ChannelId == channelId)
            .OrderBy(p => p.ScheduledAtUtc)
            .ToListAsync();
        
        return posts.Select(t => t.ToResponse(_urlService)).ToList();
    }
    
    public async Task<PostResponse> CreateAsync(long channelId, CreatePostRequest request, long userId)
    {
        await _accessService.GetOwnedChannelAsync(channelId, userId);
        var date = _dateValidator.ParseAndValidate(request.ScheduledAtUtc);
        
        if (request.Files.Count > 10)
            throw new ValidationException("Too many attachments (max 10)");

        request.Files.ForEach(f => _fileValidator.ValidateFile(f));

        var post = request.ToPost(channelId, date);

        var dir = Path.Combine(_mediaOptions.PostAttachmentsPath, Guid.NewGuid().ToString());
        for (int i = 0; i < request.Files.Count; i++)
        {
            var file = request.Files[i];
            var attachment = await SaveAttachmentAsync(file, dir, i);
            post.Attachments.Add(attachment);
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

        return fullPost.ToResponse(_urlService);
    }

    public async Task<PostResponse> UpdateAsync(long channelId, long postId, UpdatePostRequest request, long userId)
    {
        await _accessService.GetOwnedChannelAsync(channelId, userId);
        var post = await _accessService.GetOwnedPostAsync(channelId, postId, userId);
        if (post.Status != PostStatus.Scheduled) throw new ValidationException("Only scheduled posts can be edited");
        var date = _dateValidator.ParseAndValidate(request.ScheduledAtUtc);
        
        request.NewFiles.ForEach(_fileValidator.ValidateFile);
        
        var toRemove = post.Attachments.Where(a => !request.KeepAttachmentIds.Contains(a.Id)).ToList();
        foreach (var a in toRemove)
        {
            if (System.IO.File.Exists(a.StorageKey))
                System.IO.File.Delete(a.StorageKey);
            _db.PostAttachments.Remove(a);
        }

        var dir = Path.Combine(_mediaOptions.PostAttachmentsPath, post.Id.ToString());
        var startOrder = request.KeepAttachmentIds.Count;
        for (int i = 0; i < request.NewFiles.Count; i++)
        {
            var file = request.NewFiles[i];
            var attachment = await SaveAttachmentAsync(file, dir, startOrder + i);
            post.Attachments.Add(attachment);
        }

        if (request.Text != null) post.Text = request.Text;
        if (request.ScheduledAtUtc?.Length != 0 && date != post.ScheduledAtUtc)
        {
            if (date <= DateTime.UtcNow)
                throw new ValidationException("Scheduled time must be in the future.");

            post.ScheduledAtUtc = date;

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
        return updated.ToResponse(_urlService);
    }

    public async void DeleteAsync(long channelId, long postId, long userId)
    {
        await _accessService.GetOwnedChannelAsync(channelId, userId);
        var post = await _accessService.GetOwnedPostAsync(channelId, postId, userId);
        if (!string.IsNullOrEmpty(post.HangfireJobId))
            BackgroundJob.Delete(post.HangfireJobId);
        
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
    }
    
    private async Task<PostAttachment> SaveAttachmentAsync(IFormFile file, string directory, int order)
    {
        Directory.CreateDirectory(directory);
        var storageKey = Path.Combine(directory, $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}");
        await using (var stream = new FileStream(storageKey, FileMode.Create))
            await file.CopyToAsync(stream);

        return new PostAttachment
        {
            FileName = file.FileName,
            MimeType = file.ContentType,
            Size = file.Length,
            StorageProvider = StorageProvider.Local,
            StorageKey = storageKey,
            Order = order
        };
    }
}
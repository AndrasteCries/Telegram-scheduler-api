using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Services.Posts;
using TelegramScheduler.Services.Templates;

namespace TelegramScheduler.Controllers.telegram;

[Authorize]
[ApiController]
[Route("api/channels")]
public class TelegramChannelsController : ControllerBase
{
    private readonly PostService _postService;
    
    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public TelegramChannelsController(PostService postService)
    {
        _postService = postService;
    }

    [HttpGet("{channelId}/posts")]
    public async Task<ActionResult<List<PostResponse>>> GetPosts(long channelId, [FromQuery] DateTime from,
        [FromQuery] DateTime to)
    {
        var posts = _postService.GetPosts(channelId, CurrentUserId, from, to);
        
        return Ok(posts);
    }

    [HttpPost("{channelId}/posts")]
    public async Task<ActionResult<PostResponse>> CreatePost(long channelId, [FromForm] CreatePostRequest request)
    {
        var post = await _postService.CreateAsync(channelId, request, CurrentUserId);

        return Ok(post);
    }

    [HttpPut("{channelId}/posts/{postId}")]
    public async Task<ActionResult<PostResponse>> UpdatePost(long channelId, long postId,
        UpdatePostRequest request)
    {
        var post = await _postService.UpdateAsync(channelId, postId, request, CurrentUserId);

        return Ok(post);
    }

    [HttpDelete("{channelId}/posts/{postId}")]
    public async Task<IActionResult> DeletePost(long channelId, long postId)
    {
        _postService.DeleteAsync(channelId, postId, CurrentUserId);
        return NoContent();
    }

    
    // [HttpGet("~/api/attachments/{attachmentId}/file")]
    // [AllowAnonymous]
    // public async Task<IActionResult> GetAttachmentFile(long attachmentId, [FromQuery] long expires,
    //     [FromQuery] string sig)
    // {
    //     var resourcePath = $"/api/attachments/{attachmentId}/file";
    //
    //     if (!_urlService.ValidateSignedUrl(resourcePath, expires, sig))
    //         return Unauthorized();
    //
    //     var attachment = await _db.PostAttachments.FindAsync(attachmentId);
    //     if (attachment == null || !System.IO.File.Exists(attachment.StorageKey))
    //         return NotFound();
    //
    //     return PhysicalFile(Path.GetFullPath(attachment.StorageKey), attachment.MimeType);
    // }
}
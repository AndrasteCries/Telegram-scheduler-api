using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TelegramScheduler.Data;
using TelegramScheduler.Models;
using TelegramScheduler.Services;

namespace TelegramScheduler.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PostsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IBackgroundJobClient _jobs;

    public PostsController(AppDbContext db, IBackgroundJobClient jobs)
    {
        _db = db;
        _jobs = jobs;
    }

    [HttpGet]
    public async Task<ActionResult<List<Post>>> GetAll()
    {
        return await _db.Posts.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> Get(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null)
            return NotFound();
        return Ok(post);
    }

    [HttpPost]
    public async Task<ActionResult<Post>> Create(Post post)
    {
        if (DateTime.UtcNow > post.ScheduledAtUtc)
            return BadRequest();
        _db.Posts.Add(post);
        await _db.SaveChangesAsync();
        post.HangfireJobId = _jobs.Schedule<NotifierService>(n => n.NotifyByPostAsync(post.Id), post.ScheduledAtUtc - DateTime.UtcNow);
        
        await _db.SaveChangesAsync();
        
        return CreatedAtAction(
            nameof(Get),
            new { id = post.Id },
            post);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<Post>> Cancel(int id)
    {
        var post = await _db.Posts.FindAsync(id);
        if (post == null)
            return NotFound();
        if (post.HangfireJobId != null)
            _jobs.Delete(post.HangfireJobId);
        _db.Posts.Remove(post);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
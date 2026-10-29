using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TelegramScheduler.DTOs.Posts;
using TelegramScheduler.Services.Templates;

namespace TelegramScheduler.Controllers.telegram;


[Authorize]
[ApiController]
[Route("api/channels")]
public class TelegramTemplatesController : ControllerBase
{
    private readonly TemplateService _templateService;
    
    private long CurrentUserId => long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public TelegramTemplatesController(TemplateService templateService)
    {
        _templateService = templateService;
    }
    
    [HttpGet("{channelId}/slot-templates")]
    public async Task<ActionResult<List<SlotTemplateResponse>>> GetTemplates(long channelId)
    {
        var templates = _templateService.GetTemplates(channelId, CurrentUserId);
        return Ok(templates);
    }

    [HttpPost("{channelId}/slot-templates")]
    public async Task<ActionResult<List<SlotTemplateResponse>>> UpdateTemplates(
        long channelId,
        List<UpsertSlotTemplateRequest> request)
    {
        var updatedTemplates = _templateService.UpdateTemplates(channelId, request, CurrentUserId);

        return Ok(updatedTemplates);
    }
    
}
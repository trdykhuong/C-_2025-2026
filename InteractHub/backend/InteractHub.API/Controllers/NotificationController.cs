using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : BaseController
{
    private readonly NotificationService _notifService;
    public NotificationController(NotificationService notifService) => _notifService = notifService;

    // GET /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _notifService.GetAllAsync(CurrentUserId));

    // PUT /api/notifications/{id}/read
    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var result = await _notifService.MarkReadAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/notifications/read-all
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _notifService.MarkAllReadAsync(CurrentUserId);
        return Ok(new { success = true });
    }
}

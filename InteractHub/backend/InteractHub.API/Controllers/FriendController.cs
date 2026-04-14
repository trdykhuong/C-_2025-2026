using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public class FriendController : BaseController
{
    private readonly FriendService _friendService;
    public FriendController(FriendService friendService) => _friendService = friendService;

    // GET /api/friends  – danh sách bạn bè
    [HttpGet]
    public async Task<IActionResult> GetFriends()
        => Ok(await _friendService.GetFriendsAsync(CurrentUserId));

    // GET /api/friends/requests  – lời mời đang chờ
    [HttpGet("requests")]
    public async Task<IActionResult> GetPending()
        => Ok(await _friendService.GetPendingAsync(CurrentUserId));

    // POST /api/friends/request
    [HttpPost("request")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDTO dto)
    {
        var result = await _friendService.SendRequestAsync(CurrentUserId, dto.ReceiverId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // PUT /api/friends/request/{id}?accept=true
    [HttpPut("request/{id:int}")]
    public async Task<IActionResult> Respond(int id, [FromQuery] bool accept)
    {
        var result = await _friendService.RespondAsync(id, CurrentUserId, accept);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/friends/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remove(int id)
    {
        var result = await _friendService.RemoveAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // DELETE /api/friends/with/{userId}  – hủy kết bạn / hủy lời mời bằng userId
    [HttpDelete("with/{userId}")]
    public async Task<IActionResult> RemoveByUser(string userId)
    {
        var result = await _friendService.RemoveByUserIdAsync(CurrentUserId, userId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

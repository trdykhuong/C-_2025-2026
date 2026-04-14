using InteractHub.API.DTOs;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/stories")]
[Authorize]
public class StoryController : BaseController
{
    private readonly StoryService _storyService;
    public StoryController(StoryService storyService) => _storyService = storyService;

    // GET /api/stories
    [HttpGet]
    public async Task<IActionResult> GetFeed()
        => Ok(await _storyService.GetFeedAsync(CurrentUserId));

    // POST /api/stories
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStoryDTO dto)
    {
        var result = await _storyService.CreateAsync(CurrentUserId, dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/stories/{id}/view  – ghi nhận lượt xem
    [HttpPost("{id:int}/view")]
    public async Task<IActionResult> RecordView(int id)
    {
        await _storyService.RecordViewAsync(id, CurrentUserId);
        return Ok();
    }

    // DELETE /api/stories/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _storyService.DeleteAsync(id, CurrentUserId);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

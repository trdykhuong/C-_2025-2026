using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;

namespace InteractHub.API.Services;

public class StoryService
{
    private readonly IStoryRepository _repo;
    public StoryService(IStoryRepository repo) => _repo = repo;

    public async Task<List<StoryResponseDTO>> GetFeedAsync(string userId)
    {
        var friendIds = await _repo.GetFriendIdsAsync(userId);
        friendIds.Add(userId);

        var stories = await _repo.GetFeedAsync(friendIds, userId);
        return stories.Select(s => new StoryResponseDTO
        {
            Id         = s.Id,
            ImageUrl   = s.ImageUrl,
            Caption    = s.Caption,
            Visibility = s.Visibility,
            CreatedAt  = s.CreatedAt,
            ExpiresAt  = s.ExpiresAt,
            ViewCount  = s.StoryViews.Count,
            Author     = new UserSummaryDTO
            {
                Id        = s.User.Id,
                UserName  = s.User.UserName!,
                FullName  = s.User.FullName,
                AvatarUrl = s.User.AvatarUrl,
            },
        }).ToList();
    }

    public async Task<ApiResult<StoryResponseDTO>> CreateAsync(string userId, CreateStoryDTO dto)
    {
        var story = new Story
        {
            UserId     = userId,
            ImageUrl   = dto.ImageUrl,
            Caption    = dto.Caption,
            Visibility = dto.Visibility,
        };
        _repo.Add(story);
        await _repo.SaveChangesAsync();
        await _repo.LoadUserAsync(story);

        return ApiResult<StoryResponseDTO>.Ok(new StoryResponseDTO
        {
            Id         = story.Id,
            ImageUrl   = story.ImageUrl,
            Caption    = story.Caption,
            Visibility = story.Visibility,
            CreatedAt  = story.CreatedAt,
            ExpiresAt  = story.ExpiresAt,
            ViewCount  = 0,
            Author     = new UserSummaryDTO
            {
                Id        = story.User.Id,
                UserName  = story.User.UserName!,
                FullName  = story.User.FullName,
                AvatarUrl = story.User.AvatarUrl,
            },
        });
    }

    public async Task RecordViewAsync(int storyId, string userId)
    {
        var story = await _repo.FindByIdAsync(storyId);
        if (story == null || story.UserId == userId) return; // không tính lượt xem của chính mình

        if (!await _repo.HasViewedAsync(storyId, userId))
        {
            _repo.AddView(new StoryView { StoryId = storyId, UserId = userId });
            await _repo.SaveChangesAsync();
        }
    }

    public async Task<ApiResult<bool>> DeleteAsync(int storyId, string userId)
    {
        var story = await _repo.FindByIdAsync(storyId);
        if (story == null)          return ApiResult<bool>.Fail("Không tìm thấy story.");
        if (story.UserId != userId) return ApiResult<bool>.Fail("Không có quyền xóa.");

        _repo.Remove(story);
        await _repo.SaveChangesAsync();
        return ApiResult<bool>.Ok(true);
    }
}

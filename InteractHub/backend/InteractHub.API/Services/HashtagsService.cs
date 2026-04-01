using InteractHub.API.Data;
using InteractHub.API.DTOs;
using Microsoft.EntityFrameworkCore;

namespace InteractHub.API.Services;

public interface IHashtagsService
{
    Task<List<HashtagTrendDto>> GetTrendingAsync(int count = 10);
}

public class HashtagsService : IHashtagsService
{
    private readonly AppDbContext _db;
    public HashtagsService(AppDbContext db) => _db = db;

    public async Task<List<HashtagTrendDto>> GetTrendingAsync(int count = 10)
    {
        return await _db.PostHashtags
            .GroupBy(ph => ph.Hashtag.Name)
            .Select(g => new HashtagTrendDto
            {
                Name = g.Key,
                PostCount = g.Count()
            })
            .OrderByDescending(h => h.PostCount)
            .Take(count)
            .ToListAsync();
    }
}

// Add this DTO to DTOs file
namespace InteractHub.API.DTOs
{
    public class HashtagTrendDto
    {
        public string Name { get; set; } = string.Empty;
        public int PostCount { get; set; }
    }
}

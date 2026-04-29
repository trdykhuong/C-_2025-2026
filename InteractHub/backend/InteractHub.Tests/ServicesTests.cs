using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Repositories.Interfaces;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace InteractHub.Tests;

// ─── HELPERS ────────────────────────────────────────────────────────────────
public static class TestHelpers
{
    public static AppUser CreateUser(string id = "user1", string name = "Test User") => new()
    {
        Id       = id,
        FullName = name,
        UserName = id,
        Email    = $"{id}@test.com"
    };

    // Tạo NotificationService với tất cả dependency đã được mock
    public static NotificationService CreateNotifService(Mock<INotificationRepository> notifRepo)
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(r => r.FindByIdAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var pusher = new Mock<IRealtimePusher>();
        pusher.Setup(p => p.PushNotificationAsync(It.IsAny<string>(), It.IsAny<NotificationResponseDTO>()))
              .Returns(Task.CompletedTask);

        return new NotificationService(notifRepo.Object, userRepo.Object, pusher.Object);
    }
}

// ─── AUTH SERVICE TESTS ─────────────────────────────────────────────────────
public class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IConfiguration>       _configMock;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        var jwtSection = new Mock<IConfigurationSection>();
        jwtSection.Setup(s => s["Key"]).Returns("TestSecretKey_Min32Chars_ForTesting!!");
        jwtSection.Setup(s => s["Issuer"]).Returns("TestIssuer");
        jwtSection.Setup(s => s["Audience"]).Returns("TestAudience");
        jwtSection.Setup(s => s["ExpiresHours"]).Returns("24");

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsFailure()
    {
        var existingUser = TestHelpers.CreateUser();
        _userManagerMock.Setup(m => m.FindByEmailAsync("existing@test.com")).ReturnsAsync(existingUser);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);
        var result  = await service.RegisterAsync(new RegisterDTO
        {
            Email    = "existing@test.com",
            Password = "Test1234",
            FullName = "Test User",
            UserName = "testuser"
        });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManagerMock.Setup(m => m.FindByNameAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);
        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>())).ReturnsAsync(["User"]);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);
        var result  = await service.RegisterAsync(new RegisterDTO
        {
            Email    = "new@test.com",
            Password = "Test1234!",
            FullName = "New User",
            UserName = "newuser"
        });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsFailure()
    {
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>())).ReturnsAsync((AppUser?)null);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);
        var result  = await service.LoginAsync(new LoginDTO { Email = "wrong@test.com", Password = "WrongPass" });

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Login_WithDeactivatedAccount_ReturnsFailure()
    {
        var user = TestHelpers.CreateUser();
        user.IsActive = false;

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass1234")).ReturnsAsync(true);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);
        var result  = await service.LoginAsync(new LoginDTO { Email = user.Email!, Password = "Pass1234" });

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}

// ─── POSTS SERVICE TESTS ────────────────────────────────────────────────────
public class PostsServiceTests
{
    private static (PostService service, Mock<IPostRepository> repo) MakeService()
    {
        var repo      = new Mock<IPostRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        return (new PostService(repo.Object, TestHelpers.CreateNotifService(notifRepo)), repo);
    }

    [Fact]
    public async Task CreatePost_WithValidData_ReturnsPost()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser();

        repo.Setup(r => r.FindHashtagByNameAsync(It.IsAny<string>())).ReturnsAsync((Hashtag?)null);
        repo.Setup(r => r.LoadUserAsync(It.IsAny<Post>()))
            .Callback<Post>(p => p.User = user)
            .Returns(Task.CompletedTask);

        var result = await service.CreateAsync(user.Id, new CreatePostDTO
        {
            Content  = "Hello World!",
            Hashtags = ["test"]
        });

        Assert.True(result.Success);
        Assert.Equal("Hello World!", result.Data!.Content);
    }

    [Fact]
    public async Task DeletePost_ByNonOwner_ReturnsUnauthorized()
    {
        var (service, repo) = MakeService();
        var owner = TestHelpers.CreateUser("owner");
        var post  = new Post { Id = 1, Content = "Test", UserId = owner.Id, User = owner };

        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(post);

        var result = await service.DeleteAsync(1, "other");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task ToggleLike_FirstTime_CreatesLike()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser();
        var post = new Post { Id = 1, Content = "Like me", UserId = "owner", User = user };

        repo.Setup(r => r.FindByIdAsync(1)).ReturnsAsync(post);
        repo.Setup(r => r.FindLikeAsync(1, user.Id)).ReturnsAsync((Like?)null);

        var result = await service.ToggleLikeAsync(1, user.Id);

        Assert.True(result.Success);
        Assert.True(result.Data);
        repo.Verify(r => r.AddLike(It.IsAny<Like>()), Times.Once);
    }

    [Fact]
    public async Task SearchPosts_WithMatchingContent_ReturnsResults()
    {
        var (service, repo) = MakeService();
        var user = TestHelpers.CreateUser();
        var post = new Post { Id = 1, Content = "Hello world", UserId = user.Id, User = user };

        repo.Setup(r => r.SearchPagedAsync("Hello", 1, 10))
            .ReturnsAsync((1, new List<Post> { post }));

        var result = await service.SearchAsync("Hello", user.Id, 1, 10);

        Assert.Single(result.Items);
        Assert.Contains("Hello", result.Items.First().Content);
    }
}

// ─── FRIENDS SERVICE TESTS ──────────────────────────────────────────────────
public class FriendsServiceTests
{
    private static (FriendService service, Mock<IFriendRepository> repo) MakeService()
    {
        var repo      = new Mock<IFriendRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        var notifRepo = new Mock<INotificationRepository>();
        notifRepo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        return (new FriendService(repo.Object, TestHelpers.CreateNotifService(notifRepo)), repo);
    }

    [Fact]
    public async Task SendRequest_ToSelf_ReturnsFailure()
    {
        var (service, _) = MakeService();
        var result = await service.SendRequestAsync("user1", "user1");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task SendRequest_ToNonExistentUser_ReturnsFailure()
    {
        var (service, repo) = MakeService();
        repo.Setup(r => r.FindUserAsync("nonexistent")).ReturnsAsync((AppUser?)null);

        var result = await service.SendRequestAsync("user1", "nonexistent");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public async Task SendRequest_ValidRequest_CreatesFriendship()
    {
        var (service, repo) = MakeService();
        var sender   = TestHelpers.CreateUser("sender");
        var receiver = TestHelpers.CreateUser("receiver");

        repo.Setup(r => r.FindUserAsync(receiver.Id)).ReturnsAsync(receiver);
        repo.Setup(r => r.ExistsAsync(sender.Id, receiver.Id)).ReturnsAsync(false);

        var result = await service.SendRequestAsync(sender.Id, receiver.Id);

        Assert.True(result.Success);
        Assert.Equal("pending", result.Data!.Status);
    }

    [Fact]
    public async Task RespondToRequest_Accept_UpdatesStatus()
    {
        var (service, repo) = MakeService();
        var sender     = TestHelpers.CreateUser("s1");
        var receiver   = TestHelpers.CreateUser("r1");
        var friendship = new Friendship
        {
            Id = 1, SenderId = sender.Id, ReceiverId = receiver.Id,
            Sender = sender, Receiver = receiver
        };

        repo.Setup(r => r.FindByIdWithUsersAsync(1)).ReturnsAsync(friendship);

        var result = await service.RespondAsync(1, receiver.Id, true);

        Assert.True(result.Success);
        Assert.Equal("accepted", result.Data!.Status);
    }
}

// ─── NOTIFICATIONS SERVICE TESTS ────────────────────────────────────────────
public class NotificationsServiceTests
{
    private static (NotificationService service, Mock<INotificationRepository> repo) MakeService()
    {
        var repo = new Mock<INotificationRepository>();
        repo.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);
        return (TestHelpers.CreateNotifService(repo), repo);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyUserNotifications()
    {
        var (service, repo) = MakeService();
        var notifs = new List<Notification>
        {
            new() { Id = 1, UserId = "u1", Message = "For user", Type = "like" }
        };
        repo.Setup(r => r.GetByUserAsync("u1")).ReturnsAsync(notifs);

        var result = await service.GetAllAsync("u1");

        Assert.Single(result);
        Assert.Equal("For user", result.First().Message);
    }

    [Fact]
    public async Task MarkAllAsRead_CallsRepository()
    {
        var (service, repo) = MakeService();
        repo.Setup(r => r.MarkAllReadAsync("u1")).Returns(Task.CompletedTask);

        await service.MarkAllReadAsync("u1");

        repo.Verify(r => r.MarkAllReadAsync("u1"), Times.Once);
    }
}

using InteractHub.API.Data;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using InteractHub.API.Services;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace InteractHub.Tests;

// ─── HELPERS ────────────────────────────────────────────────────────────────
public static class TestHelpers
{
    public static AppDbContext CreateInMemoryDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    public static AppUser CreateUser(string id = "user1", string name = "Test User") => new AppUser
    {
        Id = id,
        FullName = name,
        UserName = id,
        Email = $"{id}@test.com"
    };
}

// ─── AUTH SERVICE TESTS ─────────────────────────────────────────────────────
public class AuthServiceTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IConfigurationSection> _jwtSectionMock;

    public AuthServiceTests()
    {
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _jwtSectionMock = new Mock<IConfigurationSection>();
        _jwtSectionMock.Setup(s => s["Key"]).Returns("TestSecretKey_Min32Chars_ForTesting!!");
        _jwtSectionMock.Setup(s => s["Issuer"]).Returns("TestIssuer");
        _jwtSectionMock.Setup(s => s["Audience"]).Returns("TestAudience");
        _jwtSectionMock.Setup(s => s["ExpiresHours"]).Returns("24");

        _configMock = new Mock<IConfiguration>();
        _configMock.Setup(c => c.GetSection("Jwt")).Returns(_jwtSectionMock.Object);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        var existingUser = TestHelpers.CreateUser();
        _userManagerMock.Setup(m => m.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(existingUser);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);

        // Act
        var result = await service.RegisterAsync(new RegisterDto
        {
            Email = "existing@test.com",
            Password = "Test1234",
            FullName = "Test User",
            UserName = "testuser"
        });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Email already in use", result.Errors.First());
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsSuccess()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        _userManagerMock.Setup(m => m.CreateAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.AddToRoleAsync(It.IsAny<AppUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<AppUser>()))
            .ReturnsAsync(new List<string> { "User" });

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);

        // Act
        var result = await service.RegisterAsync(new RegisterDto
        {
            Email = "new@test.com",
            Password = "Test1234!",
            FullName = "New User",
            UserName = "newuser"
        });

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data!.Token);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsFailure()
    {
        // Arrange
        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((AppUser?)null);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);

        // Act
        var result = await service.LoginAsync(new LoginDto
        {
            Email = "wrong@test.com",
            Password = "WrongPass"
        });

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Login_WithDeactivatedAccount_ReturnsFailure()
    {
        // Arrange
        var user = TestHelpers.CreateUser();
        user.IsActive = false;

        _userManagerMock.Setup(m => m.FindByEmailAsync(user.Email!)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.CheckPasswordAsync(user, "Pass1234")).ReturnsAsync(true);

        var service = new AuthService(_userManagerMock.Object, _configMock.Object);

        // Act
        var result = await service.LoginAsync(new LoginDto { Email = user.Email!, Password = "Pass1234" });

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deactivated", result.Errors.First().ToLower());
    }
}

// ─── POSTS SERVICE TESTS ────────────────────────────────────────────────────
public class PostsServiceTests
{
    private readonly Mock<INotificationsService> _notifMock = new();

    [Fact]
    public async Task CreatePost_WithValidData_ReturnsPost()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_create_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser();
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.CreateAsync(user.Id, new CreatePostDto
        {
            Content = "Hello World!",
            Hashtags = new List<string> { "test" }
        });

        Assert.True(result.Success);
        Assert.Equal("Hello World!", result.Data!.Content);
    }

    [Fact]
    public async Task DeletePost_ByNonOwner_ReturnsUnauthorized()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_delete_" + Guid.NewGuid());
        var owner = TestHelpers.CreateUser("owner");
        var other = TestHelpers.CreateUser("other");
        db.Users.AddRange(owner, other);
        var post = new Post { Content = "Test", UserId = owner.Id, User = owner };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.DeleteAsync(post.Id, other.Id);

        Assert.False(result.Success);
        Assert.Contains("Unauthorized", result.Errors.First());
    }

    [Fact]
    public async Task ToggleLike_FirstTime_CreatesLike()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_like_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser();
        db.Users.Add(user);
        var post = new Post { Content = "Like me", UserId = user.Id, User = user };
        db.Posts.Add(post);
        await db.SaveChangesAsync();

        _notifMock.Setup(n => n.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>())).Returns(Task.CompletedTask);

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.ToggleLikeAsync(post.Id, "other_user");

        Assert.True(result.Success);
        Assert.True(result.Data); // liked
    }

    [Fact]
    public async Task SearchPosts_WithMatchingContent_ReturnsResults()
    {
        var db = TestHelpers.CreateInMemoryDb("posts_search_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser();
        db.Users.Add(user);
        db.Posts.AddRange(
            new Post { Content = "Hello world", UserId = user.Id, User = user },
            new Post { Content = "Goodbye world", UserId = user.Id, User = user }
        );
        await db.SaveChangesAsync();

        var service = new PostsService(db, _notifMock.Object);
        var result = await service.SearchAsync("Hello", user.Id, 1, 10);

        Assert.Single(result.Items);
        Assert.Contains("Hello", result.Items.First().Content);
    }
}

// ─── FRIENDS SERVICE TESTS ──────────────────────────────────────────────────
public class FriendsServiceTests
{
    private readonly Mock<INotificationsService> _notifMock = new();

    public FriendsServiceTests()
    {
        _notifMock.Setup(n => n.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>())).Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task SendRequest_ToSelf_ReturnsFailure()
    {
        var db = TestHelpers.CreateInMemoryDb("friends_self_" + Guid.NewGuid());
        var service = new FriendsService(db, _notifMock.Object);

        var result = await service.SendRequestAsync("user1", "user1");

        Assert.False(result.Success);
        Assert.Contains("yourself", result.Errors.First().ToLower());
    }

    [Fact]
    public async Task SendRequest_ToNonExistentUser_ReturnsFailure()
    {
        var db = TestHelpers.CreateInMemoryDb("friends_notfound_" + Guid.NewGuid());
        var service = new FriendsService(db, _notifMock.Object);

        var result = await service.SendRequestAsync("user1", "nonexistent");

        Assert.False(result.Success);
        Assert.Contains("not found", result.Errors.First().ToLower());
    }

    [Fact]
    public async Task SendRequest_ValidRequest_CreatesFriendship()
    {
        var db = TestHelpers.CreateInMemoryDb("friends_valid_" + Guid.NewGuid());
        var sender = TestHelpers.CreateUser("sender");
        var receiver = TestHelpers.CreateUser("receiver");
        db.Users.AddRange(sender, receiver);
        await db.SaveChangesAsync();

        var service = new FriendsService(db, _notifMock.Object);
        var result = await service.SendRequestAsync(sender.Id, receiver.Id);

        Assert.True(result.Success);
        Assert.Equal("pending", result.Data!.Status);
    }

    [Fact]
    public async Task RespondToRequest_Accept_UpdatesStatus()
    {
        var db = TestHelpers.CreateInMemoryDb("friends_accept_" + Guid.NewGuid());
        var sender = TestHelpers.CreateUser("s1");
        var receiver = TestHelpers.CreateUser("r1");
        db.Users.AddRange(sender, receiver);
        var friendship = new Friendship { SenderId = sender.Id, ReceiverId = receiver.Id, Sender = sender, Receiver = receiver };
        db.Friendships.Add(friendship);
        await db.SaveChangesAsync();

        var service = new FriendsService(db, _notifMock.Object);
        var result = await service.RespondToRequestAsync(friendship.Id, receiver.Id, true);

        Assert.True(result.Success);
        Assert.Equal("accepted", result.Data!.Status);
    }
}

// ─── NOTIFICATIONS SERVICE TESTS ────────────────────────────────────────────
public class NotificationsServiceTests
{
    [Fact]
    public async Task GetNotifications_ReturnsOnlyUserNotifications()
    {
        var db = TestHelpers.CreateInMemoryDb("notif_get_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        var other = TestHelpers.CreateUser("u2");
        db.Users.AddRange(user, other);
        db.Notifications.AddRange(
            new Notification { UserId = user.Id, Message = "For user", Type = "like" },
            new Notification { UserId = other.Id, Message = "For other", Type = "like" }
        );
        await db.SaveChangesAsync();

        var service = new NotificationsService(db);
        var result = await service.GetUserNotificationsAsync(user.Id);

        Assert.Single(result);
        Assert.Equal("For user", result.First().Message);
    }

    [Fact]
    public async Task MarkAllAsRead_UpdatesAllUnread()
    {
        var db = TestHelpers.CreateInMemoryDb("notif_readall_" + Guid.NewGuid());
        var user = TestHelpers.CreateUser("u1");
        db.Users.Add(user);
        db.Notifications.AddRange(
            new Notification { UserId = user.Id, Message = "A", Type = "like", IsRead = false },
            new Notification { UserId = user.Id, Message = "B", Type = "like", IsRead = false }
        );
        await db.SaveChangesAsync();

        var service = new NotificationsService(db);
        await service.MarkAllAsReadAsync(user.Id);

        var unread = db.Notifications.Count(n => n.UserId == user.Id && !n.IsRead);
        Assert.Equal(0, unread);
    }
}

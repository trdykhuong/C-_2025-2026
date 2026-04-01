-- ============================================================
-- InteractHub Database Creation Script
-- Run this to create the database manually (alternative to EF migrations)
-- ============================================================

CREATE DATABASE InteractHubDb;
GO

USE InteractHubDb;
GO

-- ─── ASP.NET IDENTITY TABLES ───────────────────────────────
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    Name NVARCHAR(256),
    NormalizedName NVARCHAR(256),
    ConcurrencyStamp NVARCHAR(MAX)
);

CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) NOT NULL PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Bio NVARCHAR(300),
    AvatarUrl NVARCHAR(MAX),
    CoverUrl NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsActive BIT NOT NULL DEFAULT 1,
    UserName NVARCHAR(256),
    NormalizedUserName NVARCHAR(256),
    Email NVARCHAR(256),
    NormalizedEmail NVARCHAR(256),
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX),
    SecurityStamp NVARCHAR(MAX),
    ConcurrencyStamp NVARCHAR(MAX),
    PhoneNumber NVARCHAR(MAX),
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEnd DATETIMEOFFSET,
    LockoutEnabled BIT NOT NULL DEFAULT 1,
    AccessFailedCount INT NOT NULL DEFAULT 0
);

CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);

CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX),
    ClaimValue NVARCHAR(MAX),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE TABLE AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX),
    PRIMARY KEY (UserId, LoginProvider, Name),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

-- ─── DOMAIN TABLES ────────────────────────────────────────────────────────────

CREATE TABLE Posts (
    Id INT IDENTITY PRIMARY KEY,
    Content NVARCHAR(2000) NOT NULL,
    ImageUrl NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    IsDeleted BIT NOT NULL DEFAULT 0,
    UserId NVARCHAR(450) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE TABLE Comments (
    Id INT IDENTITY PRIMARY KEY,
    Content NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IsDeleted BIT NOT NULL DEFAULT 0,
    UserId NVARCHAR(450) NOT NULL,
    PostId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),   -- RESTRICT
    FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
);

CREATE TABLE Likes (
    Id INT IDENTITY PRIMARY KEY,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId NVARCHAR(450) NOT NULL,
    PostId INT NOT NULL,
    CONSTRAINT UQ_Like UNIQUE (UserId, PostId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),   -- RESTRICT
    FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE
);

CREATE TABLE Hashtags (
    Id INT IDENTITY PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE PostHashtags (
    PostId INT NOT NULL,
    HashtagId INT NOT NULL,
    PRIMARY KEY (PostId, HashtagId),
    FOREIGN KEY (PostId) REFERENCES Posts(Id) ON DELETE CASCADE,
    FOREIGN KEY (HashtagId) REFERENCES Hashtags(Id) ON DELETE CASCADE
);

CREATE TABLE Friendships (
    Id INT IDENTITY PRIMARY KEY,
    Status NVARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending | accepted | rejected
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2,
    SenderId NVARCHAR(450) NOT NULL,
    ReceiverId NVARCHAR(450) NOT NULL,
    CONSTRAINT UQ_Friendship UNIQUE (SenderId, ReceiverId),
    FOREIGN KEY (SenderId) REFERENCES AspNetUsers(Id),    -- RESTRICT
    FOREIGN KEY (ReceiverId) REFERENCES AspNetUsers(Id)   -- RESTRICT
);

CREATE TABLE Stories (
    Id INT IDENTITY PRIMARY KEY,
    ImageUrl NVARCHAR(MAX),
    Caption NVARCHAR(300),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ExpiresAt DATETIME2 NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE TABLE Notifications (
    Id INT IDENTITY PRIMARY KEY,
    Message NVARCHAR(500) NOT NULL,
    Type NVARCHAR(50) NOT NULL DEFAULT 'general',
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    RelatedPostId INT,
    UserId NVARCHAR(450) NOT NULL,
    ActorId NVARCHAR(450),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);

CREATE TABLE PostReports (
    Id INT IDENTITY PRIMARY KEY,
    Reason NVARCHAR(500) NOT NULL,
    Status NVARCHAR(20) NOT NULL DEFAULT 'pending',  -- pending | reviewed | dismissed
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UserId NVARCHAR(450) NOT NULL,
    PostId INT NOT NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (PostId) REFERENCES Posts(Id)   -- RESTRICT
);

-- ─── SEED DATA ────────────────────────────────────────────────────────────────
INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
VALUES ('role-admin-id', 'Admin', 'ADMIN', '1'),
       ('role-user-id',  'User',  'USER',  '2');

-- Default admin user (password: Admin@1234 - change in production!)
INSERT INTO AspNetUsers (Id, FullName, Email, NormalizedEmail, UserName, NormalizedUserName, EmailConfirmed, IsActive,
    CreatedAt, PasswordHash, SecurityStamp, ConcurrencyStamp)
VALUES (
    'admin-seed-id-001',
    'Admin User',
    'admin@interacthub.com',
    'ADMIN@INTERACTHUB.COM',
    'admin',
    'ADMIN',
    1, 1,
    GETUTCDATE(),
    -- BCrypt hash for 'Admin@1234' (replace with actual hash generated by Identity)
    'AQAAAAIAAYagAAAAEPlaceholderHashChangeMe==',
    NEWID(),
    NEWID()
);

INSERT INTO AspNetUserRoles (UserId, RoleId)
VALUES ('admin-seed-id-001', 'role-admin-id');

-- Sample hashtags
INSERT INTO Hashtags (Name) VALUES ('trending'), ('technology'), ('news'), ('programming'), ('dotnet');
GO

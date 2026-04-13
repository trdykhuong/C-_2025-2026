using System.Text;
using InteractHub.API.Data;
using InteractHub.API.Models;
using InteractHub.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── 1. DATABASE ──────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ─── 2. IDENTITY ─────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
{
    opt.Password.RequireDigit           = true;
    opt.Password.RequiredLength         = 8;
    opt.Password.RequireUppercase       = false;
    opt.Password.RequireNonAlphanumeric = false;
    opt.User.RequireUniqueEmail         = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ─── 3. JWT AUTH ─────────────────────────────────────────────────────────────
var jwtKey    = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAud    = builder.Configuration["Jwt:Audience"]!;

builder.Services
    .AddAuthentication(opt =>
    {
        opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAud,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        };

        // Cho phép SignalR lấy JWT từ query string
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ─── 4. CORS (cho phép React frontend) ───────────────────────────────────────
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
builder.Services.AddCors(opt =>
    opt.AddPolicy("ReactApp", policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()));

// ─── 5. SERVICES (Dependency Injection) ──────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<NotificationService>(); // NotificationService phải đăng ký trước vì PostService cần nó
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<FriendService>();
builder.Services.AddScoped<StoryService>();
builder.Services.AddScoped<UserService>();

// ─── 6. SIGNALR ──────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── 7. SWAGGER ──────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "InteractHub API", Version = "v1" });

    // Cho phép nhập JWT trong Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập token: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddControllers();

// Cho phép upload file multipart
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 52428800; // 50 MB
});

// ─── BUILD APP ────────────────────────────────────────────────────────────────
var app = builder.Build();

// Tự động tạo schema khi start
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Thêm cột/bảng mới cho DB đã tồn tại (idempotent)
    try { db.Database.ExecuteSqlRaw(
        "IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Posts' AND COLUMN_NAME='Visibility') " +
        "ALTER TABLE Posts ADD Visibility nvarchar(20) NOT NULL DEFAULT 'public'"); } catch { }

    try { db.Database.ExecuteSqlRaw(@"
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='StoryViews')
        CREATE TABLE StoryViews (
            Id int IDENTITY(1,1) NOT NULL,
            ViewedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
            StoryId int NOT NULL,
            UserId nvarchar(450) NOT NULL,
            CONSTRAINT PK_StoryViews PRIMARY KEY (Id),
            CONSTRAINT FK_StoryViews_Stories FOREIGN KEY (StoryId) REFERENCES Stories(Id) ON DELETE CASCADE,
            CONSTRAINT FK_StoryViews_Users   FOREIGN KEY (UserId)   REFERENCES AspNetUsers(Id)
        )"); } catch { }

    try { db.Database.ExecuteSqlRaw(
        "IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Stories' AND COLUMN_NAME='Visibility') " +
        "ALTER TABLE Stories ADD Visibility nvarchar(20) NOT NULL DEFAULT 'public'"); } catch { }

    // Tạo thư mục uploads nếu chưa có
    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    Directory.CreateDirectory(uploadsDir);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // phục vụ wwwroot/uploads
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Endpoint kiểm tra server còn sống
app.MapGet("/health", () => Results.Ok(new { status = "OK", time = DateTime.UtcNow }));

app.Run();

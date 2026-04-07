using InteractHub.API.Data;
using InteractHub.API.Hubs;
using InteractHub.API.Models;
using InteractHub.API.Services;
using InteractHub.API.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ─── DATABASE ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
// ─── IDENTITY ────────────────────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ─── JWT AUTHENTICATION ───────────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidateLifetime         = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer              = jwtSection["Issuer"],
        ValidAudience            = jwtSection["Audience"],
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };

    // Support JWT via SignalR query string
    options.Events = new JwtBearerEvents
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
var frontendUrl = builder.Configuration["Frontend:Url"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
        policy.WithOrigins(frontendUrl, "http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ─── SERVICES ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,          AuthService>();
builder.Services.AddScoped<IPostsService,         PostsService>();
builder.Services.AddScoped<ICommentsService,      CommentsService>();
builder.Services.AddScoped<IFriendsService,       FriendsService>();
builder.Services.AddScoped<IStoriesService,       StoriesService>();
builder.Services.AddScoped<INotificationsService, NotificationsService>();
builder.Services.AddScoped<IFileUploadService,    FileUploadService>();
builder.Services.AddScoped<IUsersService,         UsersService>();
builder.Services.AddScoped<IHashtagsService,      HashtagsService>();

// ─── SIGNALR ──────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── SWAGGER ──────────────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "InteractHub API",
        Version     = "v1",
        Description = "Social Media Web Application – Spring 2026"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Enter: Bearer {token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
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

// ─── BUILD ────────────────────────────────────────────────────────────────────
var app = builder.Build();

// Auto-migrate and seed roles on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Apply migrations first
        db.Database.Migrate();

        // Only after migrations succeeded, ensure default roles exist
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Task.Run(async () =>
        {
            var roles = new[] { "Admin", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration or role seeding failed – continuing startup.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "InteractHub API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("ReactApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();

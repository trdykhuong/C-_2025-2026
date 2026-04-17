using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InteractHub.API.DTOs;
using InteractHub.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace InteractHub.API.Services;

public class AuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration      _config;

    public AuthService(UserManager<AppUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config      = config;
    }

    // ── Đăng ký tài khoản mới ────────────────────────────────────────────────
    public async Task<ApiResult<AuthResponseDTO>> RegisterAsync(RegisterDTO dto)
    {
        // Kiểm tra email đã tồn tại chưa
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            return ApiResult<AuthResponseDTO>.Fail("Email đã được sử dụng.");

        // Kiểm tra username đã tồn tại chưa
        if (await _userManager.FindByNameAsync(dto.UserName) != null)
            return ApiResult<AuthResponseDTO>.Fail("Username đã được sử dụng.");

        var user = new AppUser
        {
            FullName = dto.FullName,
            UserName = dto.UserName,
            Email    = dto.Email,
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResult<AuthResponseDTO>.Fail(errors);
        }

        var response = await BuildTokenAsync(user);
        return ApiResult<AuthResponseDTO>.Ok(response, "Đăng ký thành công!");
    }

    // ── Đăng nhập ────────────────────────────────────────────────────────────
    public async Task<ApiResult<AuthResponseDTO>> LoginAsync(LoginDTO dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null)
            return ApiResult<AuthResponseDTO>.Fail("Email hoặc mật khẩu không đúng.");

        if (!user.IsActive)
            return ApiResult<AuthResponseDTO>.Fail("Tài khoản đã bị khóa.");

        var passwordOk = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordOk)
            return ApiResult<AuthResponseDTO>.Fail("Email hoặc mật khẩu không đúng.");

        var response = await BuildTokenAsync(user);
        return ApiResult<AuthResponseDTO>.Ok(response);
    }

    // ── Tạo JWT token ─────────────────────────────────────────────────────────
    private async Task<AuthResponseDTO> BuildTokenAsync(AppUser user)
    {
        var roles   = await _userManager.GetRolesAsync(user);
        var jwtConf = _config.GetSection("Jwt");
        var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConf["Key"]!));
        var expired = DateTime.UtcNow.AddHours(double.Parse(jwtConf["ExpiresHours"] ?? "24"));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name,           user.UserName!),
            new(ClaimTypes.Email,          user.Email!),
            new("fullName",                user.FullName),
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer:             jwtConf["Issuer"],
            audience:           jwtConf["Audience"],
            claims:             claims,
            expires:            expired,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new AuthResponseDTO
        {
            Token     = new JwtSecurityTokenHandler().WriteToken(token),
            UserId    = user.Id,
            UserName  = user.UserName!,
            FullName  = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Roles     = roles,
            ExpiredAt = expired,
        };
    }
}

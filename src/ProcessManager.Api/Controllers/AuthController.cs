using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;

namespace ProcessManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    // ── POST api/auth/login ───────────────────────────────────────────────────

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Unauthorized("Invalid username or password.");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Engineer";

        var token = GenerateJwt(user, role);
        return Ok(token);
    }

    // ── GET api/auth/me ───────────────────────────────────────────────────────

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserResponseDto>> Me()
    {
        var userName = User.Identity?.Name;
        if (userName is null) return Unauthorized();

        var user = await _userManager.FindByNameAsync(userName);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserResponseDto(
            user.Id,
            user.UserName!,
            user.Email!,
            roles.FirstOrDefault() ?? "Engineer",
            user.DisplayName));
    }

    // ── POST api/auth/register (Admin only) ───────────────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<ActionResult<UserResponseDto>> Register(RegisterRequestDto dto)
    {
        if (dto.Role != "Admin" && dto.Role != "Engineer")
            return BadRequest("Role must be 'Admin' or 'Engineer'.");

        var user = new ApplicationUser
        {
            UserName = dto.UserName,
            Email = dto.Email,
            DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim()
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        await _userManager.AddToRoleAsync(user, dto.Role);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id },
            new UserResponseDto(user.Id, user.UserName!, user.Email!, dto.Role, user.DisplayName));
    }

    // ── GET api/auth/users/picker (all authenticated users) ─────────────────
    // Returns minimal Id + DisplayName list used by the UserPicker prompt type in the wizard.

    [Authorize]
    [HttpGet("users/picker")]
    public ActionResult<IEnumerable<UserPickerDto>> GetUserPicker()
    {
        var users = _userManager.Users
            .OrderBy(u => u.DisplayName ?? u.UserName)
            .Select(u => new UserPickerDto(
                u.Id,
                u.DisplayName ?? u.UserName ?? u.Email ?? u.Id))
            .ToList();
        return Ok(users);
    }

    // ── GET api/auth/users (Admin only) ──────────────────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public ActionResult<IEnumerable<UserResponseDto>> GetUsers()
    {
        var users = _userManager.Users.ToList();
        var result = new List<UserResponseDto>();

        foreach (var u in users)
        {
            var roles = _userManager.GetRolesAsync(u).Result;
            result.Add(new UserResponseDto(u.Id, u.UserName!, u.Email!, roles.FirstOrDefault() ?? "Engineer", u.DisplayName));
        }

        return Ok(result);
    }

    // ── GET api/auth/users/{id} (Admin only) ──────────────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpGet("users/{id}")]
    public async Task<ActionResult<UserResponseDto>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserResponseDto(user.Id, user.UserName!, user.Email!, roles.FirstOrDefault() ?? "Engineer", user.DisplayName));
    }

    // ── PATCH api/auth/users/{id} (Admin only) ────────────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpPatch("users/{id}")]
    public async Task<ActionResult<UserResponseDto>> UpdateUser(string id, AdminUpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        user.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
        await _userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(dto.Role) && dto.Role is "Admin" or "Engineer")
        {
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserResponseDto(user.Id, user.UserName!, user.Email!, roles.FirstOrDefault() ?? "Engineer", user.DisplayName));
    }

    // ── DELETE api/auth/users/{id} (Admin only) ───────────────────────────────

    [Authorize(Roles = "Admin")]
    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound();

        // Prevent deleting yourself
        if (user.UserName == User.Identity?.Name)
            return BadRequest("You cannot delete your own account.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }

    // ── POST api/auth/change-password (self or Admin) ─────────────────────────

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }

    // ── PUT api/auth/profile (self) ─────────────────────────────────────

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<TokenResponseDto>> UpdateProfile(UpdateProfileRequestDto dto)
    {
        var user = await _userManager.FindByNameAsync(User.Identity!.Name!);
        if (user is null) return Unauthorized();

        user.DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? null : dto.DisplayName.Trim();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(GenerateJwt(user, roles.FirstOrDefault() ?? "Engineer"));
    }

    // ── Token generation ──────────────────────────────────────────────────────

    private TokenResponseDto GenerateJwt(ApplicationUser user, string role)
    {
        var jwtConfig = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(jwtConfig["ExpiryMinutes"] ?? "480");
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (!string.IsNullOrWhiteSpace(user.DisplayName))
            claims.Add(new Claim("display_name", user.DisplayName));

        var token = new JwtSecurityToken(
            issuer: jwtConfig["Issuer"],
            audience: jwtConfig["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return new TokenResponseDto(
            new JwtSecurityTokenHandler().WriteToken(token),
            user.UserName!,
            user.Email!,
            role,
            user.DisplayName,
            expiry);
    }
}

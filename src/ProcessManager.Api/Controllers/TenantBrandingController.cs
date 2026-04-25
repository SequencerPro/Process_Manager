using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProcessManager.Api.Data;
using ProcessManager.Api.DTOs;
using ProcessManager.Domain.Entities;

namespace ProcessManager.Api.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/tenant-branding")]
public class TenantBrandingController : ControllerBase
{
    private readonly ProcessManagerDbContext _db;
    private readonly IWebHostEnvironment _env;

    public TenantBrandingController(ProcessManagerDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<ActionResult<TenantBrandingResponseDto>> Get()
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is null) return NotFound();
        return MapToDto(branding);
    }

    [HttpPut]
    public async Task<ActionResult<TenantBrandingResponseDto>> Upsert(UpdateTenantBrandingDto dto)
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is null)
        {
            branding = new TenantBranding
            {
                CompanyName = dto.CompanyName,
                PrimaryColorHex = dto.PrimaryColorHex,
                FooterText = dto.FooterText,
            };
            _db.TenantBrandings.Add(branding);
        }
        else
        {
            branding.CompanyName = dto.CompanyName;
            branding.PrimaryColorHex = dto.PrimaryColorHex;
            branding.FooterText = dto.FooterText;
        }

        await _db.SaveChangesAsync();
        return MapToDto(branding);
    }

    [HttpPost("logo")]
    public async Task<ActionResult<TenantBrandingResponseDto>> UploadLogo(IFormFile file)
    {
        if (file.Length == 0)
            return BadRequest("No file uploaded.");
        if (file.Length > 2 * 1024 * 1024)
            return BadRequest("Logo must be under 2 MB.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".svg"))
            return BadRequest("Logo must be PNG, JPG, or SVG.");

        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding is null)
        {
            branding = new TenantBranding { CompanyName = "My Company" };
            _db.TenantBrandings.Add(branding);
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(branding.LogoFileName))
        {
            var oldPath = Path.Combine(_env.WebRootPath, "uploads", "logos", branding.LogoFileName);
            if (System.IO.File.Exists(oldPath))
                System.IO.File.Delete(oldPath);
        }

        var fileName = $"{Guid.NewGuid()}{ext}";
        var dir = Path.Combine(_env.WebRootPath, "uploads", "logos");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        branding.LogoFileName = fileName;
        await _db.SaveChangesAsync();

        return MapToDto(branding);
    }

    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo()
    {
        var branding = await _db.TenantBrandings.FirstOrDefaultAsync();
        if (branding?.LogoFileName is null) return NotFound();

        var path = Path.Combine(_env.WebRootPath, "uploads", "logos", branding.LogoFileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);

        branding.LogoFileName = null;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static TenantBrandingResponseDto MapToDto(TenantBranding b) => new(
        b.Id, b.LogoFileName, b.PrimaryColorHex, b.CompanyName,
        b.FooterText, b.CreatedAt, b.UpdatedAt);
}

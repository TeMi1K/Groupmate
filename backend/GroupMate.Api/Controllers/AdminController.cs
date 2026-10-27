using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using GroupMate.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>
/// Administrēšanas modulis (prasības 10.1., 10.5.).
/// Prototipā realizēta tikai datu nolasīšana; lomu maiņa, bloķēšana un prasmju kataloga
/// rediģēšana saskarnē ir parādīta vizuāli.
/// </summary>
[Route("api/admin")]
public class AdminController : AppControllerBase
{
    public AdminController(AppDbContext db) : base(db) { }

    [HttpGet("users")]
    public async Task<IActionResult> Users()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();
        if (me.Role != UserRole.Administrators) return NoAccess();

        var users = await Db.Users.OrderBy(u => u.LastName).ToListAsync();
        return Ok(users
            .Select(u => new AdminUserDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsBlocked, u.CreatedAt))
            .ToList());
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();
        if (me.Role != UserRole.Administrators) return NoAccess();

        var today = Today;
        return Ok(new StatsDto(
            await Db.Users.CountAsync(),
            await Db.Users.CountAsync(u => u.Role == UserRole.Dalibnieks),
            await Db.Users.CountAsync(u => u.Role == UserRole.Organizators),
            await Db.Projects.CountAsync(),
            await Db.Projects.CountAsync(p => p.Status == ProjectStatus.Atverts && p.ApplicationDeadline >= today),
            await Db.Teams.CountAsync(t => t.IsApproved),
            await Db.Applications.CountAsync(a => a.Status == ApplicationStatus.Aktivs)));
    }
}

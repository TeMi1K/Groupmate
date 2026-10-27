using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>Dalībnieka komandas un paziņojumi (prasības 7.1., 8.1.).</summary>
[Route("api/my")]
public class MyController : AppControllerBase
{
    public MyController(AppDbContext db) : base(db) { }

    /// <summary>
    /// Dalībnieka apstiprinātās komandas. Komandas biedru e-pasti tiek atklāti tikai apstiprinātās komandās
    /// (prasība 7.1.); neapstiprināti saskaņošanas priekšlikumi dalībniekiem netiek rādīti.
    /// </summary>
    [HttpGet("teams")]
    public async Task<IActionResult> Teams()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var teams = await Db.Teams
            .Where(t => t.Members.Any(m => m.UserId == me.Id))
            .Include(t => t.Project)
            .Include(t => t.Members).ThenInclude(m => m.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .AsSplitQuery()
            .OrderByDescending(t => t.ApprovedAt)
            .ToListAsync();

        var today = Today;
        var result = teams
            .Where(t => t.IsApproved)
            .Select(t => new MyTeamDto(
                t.Id,
                t.Name,
                t.ProjectId,
                t.Project.Title,
                t.Project.EndDate,
                t.Project.EndDate < today,
                t.Members.First(m => m.UserId == me.Id).Role,
                t.Score,
                t.ApprovedAt,
                t.Members
                    .OrderBy(m => m.User.FirstName)
                    .Select(m => new TeamMemberDto(
                        m.UserId,
                        m.User.FullName,
                        m.Role,
                        t.IsApproved ? m.User.Email : null,
                        m.User.Skills
                            .OrderByDescending(s => s.Level)
                            .Take(3)
                            .Select(s => new MemberSkillDto(s.Skill.Name, s.Level))
                            .ToList()))
                    .ToList()))
            .ToList();

        return Ok(result);
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var list = await Db.Notifications
            .Where(n => n.UserId == me.Id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(n.Id, n.Title, n.Message, n.IsRead, n.EmailStatus, n.CreatedAt))
            .ToListAsync();

        return Ok(list);
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var unread = await Db.Notifications.Where(n => n.UserId == me.Id && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        await Db.SaveChangesAsync();

        return Ok(new { message = "Visi paziņojumi atzīmēti kā izlasīti." });
    }
}

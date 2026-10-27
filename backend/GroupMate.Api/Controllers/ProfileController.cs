using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using GroupMate.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>
/// Profila pārvaldības modulis (prasības 3.1.–3.4., projektējums 5.1.2)
/// un personas datu modulis (prasības 9.1., 9.2., 9.4., projektējums 5.1.8).
/// </summary>
[Route("api/profile")]
public class ProfileController : AppControllerBase
{
    public ProfileController(AppDbContext db) : base(db) { }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var user = await LoadUserAsync(me.Id);
        return Ok(ToDto(user));
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateProfileRequest req)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        // C1 – obligātās sadaļas.
        var skills = req.Skills ?? new List<SkillLevelRequest>();
        var availability = req.Availability ?? new List<AvailabilityDto>();
        var interests = (req.Interests ?? new List<string>())
            .Select(i => i.Trim())
            .Where(i => i.Length > 0)
            .Distinct()
            .ToList();

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(req.FirstName)) missing.Add("vārds");
        if (string.IsNullOrWhiteSpace(req.LastName)) missing.Add("uzvārds");
        if (string.IsNullOrWhiteSpace(req.PreferredRole)) missing.Add("vēlamā loma");
        if (skills.Count == 0) missing.Add("vismaz viena prasme");
        if (availability.Count == 0) missing.Add("vismaz viens pieejamā laika intervāls");
        if (missing.Count > 0)
            return Validation($"Nav aizpildītas obligātās profila sadaļas: {string.Join(", ", missing)}.");

        // C2 – prasmju līmenis 1–5.
        if (skills.Any(s => s.Level < 1 || s.Level > 5))
            return Validation("Prasmes līmenim jābūt robežās no 1 līdz 5.");

        if (skills.Select(s => s.SkillId).Distinct().Count() != skills.Count)
            return Validation("Katru prasmi var norādīt tikai vienu reizi.");

        var skillIds = skills.Select(s => s.SkillId).ToList();
        var existingSkills = await Db.Skills.CountAsync(s => skillIds.Contains(s.Id));
        if (existingSkills != skills.Count)
            return Validation("Izvēlētā prasme nav atrasta prasmju katalogā.");

        if (!SeedData.AllRoles.Contains(req.PreferredRole))
            return Validation("Nezināma vēlamā loma.");

        foreach (var a in availability)
        {
            if (a.DayOfWeek < 1 || a.DayOfWeek > 7)
                return Validation("Nekorekta nedēļas diena.");
            if (a.StartHour < 0 || a.EndHour > 24 || a.StartHour >= a.EndHour)
                return Validation("Pieejamā laika intervālā sākuma laikam jābūt pirms beigu laika (0–24).");
        }

        // C3 – lietotājs rediģē tikai savu profilu (ID ņemts no sesijas, nevis no pieprasījuma).
        var user = await LoadUserAsync(me.Id);
        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();
        user.PreferredRole = req.PreferredRole;
        user.Interests = interests;

        // Prasmes: esošās atjaunojam, noņemtās dzēšam, jaunās pievienojam.
        foreach (var existing in user.Skills.ToList())
        {
            var requested = skills.FirstOrDefault(s => s.SkillId == existing.SkillId);
            if (requested is null) Db.UserSkills.Remove(existing);
            else existing.Level = requested.Level;
        }
        var existingIds = user.Skills.Select(s => s.SkillId).ToHashSet();
        foreach (var s in skills.Where(s => !existingIds.Contains(s.SkillId)))
            Db.UserSkills.Add(new UserSkill { UserId = user.Id, SkillId = s.SkillId, Level = s.Level });

        // Pieejamais laiks tiek pārrakstīts pilnībā.
        Db.Availability.RemoveRange(user.Availability.ToList());
        Db.Availability.AddRange(availability.Select(a => new Availability
        {
            UserId = user.Id,
            DayOfWeek = a.DayOfWeek,
            StartHour = a.StartHour,
            EndHour = a.EndHour
        }));

        await Db.SaveChangesAsync();
        Db.ChangeTracker.Clear();

        var saved = await LoadUserAsync(me.Id);
        return Ok(ToDto(saved));
    }

    /// <summary>Visu par lietotāju glabāto datu lejupielāde JSON formātā (prasība 9.4.).</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export()
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var user = await LoadUserAsync(me.Id);

        var applications = await Db.Applications
            .Where(a => a.UserId == me.Id)
            .Select(a => new { projekts = a.Project.Title, statuss = a.Status.ToString(), izveidots = a.CreatedAt })
            .ToListAsync();

        var teams = await Db.TeamMembers
            .Where(tm => tm.UserId == me.Id && tm.Team.IsApproved)
            .Select(tm => new { projekts = tm.Team.Project.Title, komanda = tm.Team.Name, loma = tm.Role })
            .ToListAsync();

        var notifications = await Db.Notifications
            .Where(n => n.UserId == me.Id)
            .OrderBy(n => n.CreatedAt)
            .Select(n => new { n.Title, n.Message, n.CreatedAt })
            .ToListAsync();

        var export = new
        {
            eksportets = DateTime.UtcNow,
            lietotajs = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                loma = user.Role.ToString(),
                user.ConsentGiven,
                user.CreatedAt
            },
            profils = new
            {
                user.PreferredRole,
                user.Interests,
                prasmes = user.Skills.Select(s => new { s.Skill.Name, s.Level }),
                pieejamiba = user.Availability.Select(a => new { a.DayOfWeek, a.StartHour, a.EndHour })
            },
            pieteikumi = applications,
            komandas = teams,
            pazinojumi = notifications
        };

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });

        return File(Encoding.UTF8.GetBytes(json), "application/json", "groupmate-mani-dati.json");
    }

    private async Task<User> LoadUserAsync(int id) =>
        await Db.Users
            .Include(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(u => u.Availability)
            .AsSplitQuery()
            .FirstAsync(u => u.Id == id);

    private static ProfileDto ToDto(User u) => new(
        u.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Role.ToString(),
        u.PreferredRole,
        u.Interests,
        u.Skills
            .OrderByDescending(s => s.Level)
            .ThenBy(s => s.Skill.Name)
            .Select(s => new UserSkillDto(s.SkillId, s.Skill.Name, s.Skill.Category, s.Level))
            .ToList(),
        u.Availability
            .OrderBy(a => a.DayOfWeek)
            .ThenBy(a => a.StartHour)
            .Select(a => new AvailabilityDto(a.DayOfWeek, a.StartHour, a.EndHour))
            .ToList());
}

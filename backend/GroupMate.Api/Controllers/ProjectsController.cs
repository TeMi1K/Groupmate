using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using GroupMate.Api.Models;
using GroupMate.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>
/// Projektu un pieteikumu modulis (prasības 4.1., 5.1.–5.3., 11.1., 11.3.; projektējums 5.1.3, 5.1.4).
/// </summary>
[Route("api/projects")]
public class ProjectsController : AppControllerBase
{
    public ProjectsController(AppDbContext db) : base(db) { }

    /// <summary>
    /// Projektu saraksts ar meklēšanu pēc nosaukuma un filtrēšanu pēc prasmes.
    /// scope=mine – organizatora paša projekti; onlyOpen=true – tikai projekti, kuriem var pieteikties.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int? skillId,
        [FromQuery] bool onlyOpen = false,
        [FromQuery] string? scope = null)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var query = ProjectsWithDetails();

        if (scope == "mine")
            query = query.Where(p => p.OrganizerId == me.Id);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Title, pattern));
        }

        if (skillId is not null)
            query = query.Where(p => p.RequiredSkills.Any(rs => rs.SkillId == skillId));

        if (onlyOpen)
        {
            var today = Today;
            query = query.Where(p => p.Status == ProjectStatus.Atverts && p.ApplicationDeadline >= today);
        }

        var projects = await query.OrderBy(p => p.ApplicationDeadline).ToListAsync();
        return Ok(projects.Select(p => ToDto(p, me.Id)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await ProjectsWithDetails().FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");

        return Ok(ToDto(project, me.Id));
    }

    /// <summary>Projekta izveide un publicēšana (prasība 4.1., projektējums 5.1.3).</summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateProjectRequest req)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        // C4 – organizatora tiesības.
        if (me.Role != UserRole.Organizators)
            return NoAccess("Projektus var izveidot tikai organizators.");

        var requiredSkills = req.RequiredSkills ?? new List<RequiredSkillRequest>();
        var requiredRoles = (req.RequiredRoles ?? new List<string>()).Distinct().ToList();

        // C1 – pamatdati.
        if (string.IsNullOrWhiteSpace(req.Title) || string.IsNullOrWhiteSpace(req.Description))
            return Validation("Jānorāda projekta nosaukums un apraksts.");
        if (requiredSkills.Count == 0)
            return Validation("Jānorāda vismaz viena nepieciešamā prasme.");
        if (requiredRoles.Count == 0)
            return Validation("Jānorāda vismaz viena nepieciešamā loma.");

        // C2 – komandas lielums 2–10 (prasība 11.1.).
        if (req.TeamSize < 2 || req.TeamSize > 10)
            return Validation("Komandas lielumam jābūt no 2 līdz 10 dalībniekiem.");

        // C3 – termiņi.
        if (req.ApplicationDeadline < Today)
            return Validation("Pieteikšanās termiņš nevar būt pagātnē.");
        if (req.ApplicationDeadline >= req.EndDate)
            return Validation("Pieteikšanās termiņam jābūt pirms projekta beigu datuma.");

        if (requiredSkills.Any(s => s.MinLevel < 1 || s.MinLevel > 5))
            return Validation("Prasmes minimālajam līmenim jābūt no 1 līdz 5.");
        if (requiredSkills.Select(s => s.SkillId).Distinct().Count() != requiredSkills.Count)
            return Validation("Katru prasmi var norādīt tikai vienu reizi.");

        var skillIds = requiredSkills.Select(s => s.SkillId).ToList();
        if (await Db.Skills.CountAsync(s => skillIds.Contains(s.Id)) != requiredSkills.Count)
            return Validation("Izvēlētā prasme nav atrasta prasmju katalogā.");
        if (requiredRoles.Any(r => !SeedData.AllRoles.Contains(r)))
            return Validation("Nezināma loma.");

        // D5 – saglabāt projektu un prasības.
        var project = new Project
        {
            Title = req.Title.Trim(),
            Description = req.Description.Trim(),
            TeamSize = req.TeamSize,
            ApplicationDeadline = req.ApplicationDeadline,
            EndDate = req.EndDate,
            OrganizerId = me.Id,
            RequiredRoles = requiredRoles,
            RequiredSkills = requiredSkills
                .Select(s => new ProjectSkill { SkillId = s.SkillId, MinLevel = s.MinLevel })
                .ToList()
        };
        Db.Projects.Add(project);
        await Db.SaveChangesAsync();

        var created = await ProjectsWithDetails().FirstAsync(p => p.Id == project.Id);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, ToDto(created, me.Id));
    }

    /// <summary>Pieteikšanās projektam (prasība 5.2., projektējums 5.1.4).</summary>
    [HttpPost("{id:int}/applications")]
    public async Task<IActionResult> Apply(int id)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();
        if (me.Role != UserRole.Dalibnieks)
            return NoAccess("Projektiem var pieteikties tikai dalībnieki.");

        var project = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");

        // C1 – projekts atvērts pieteikumiem (prasība 11.3.).
        if (!project.IsOpenForApplications(Today))
            return Validation($"Pieteikšanās termiņš ir beidzies ({project.ApplicationDeadline:dd.MM.yyyy}).");

        // C2 – pieteikums jau eksistē.
        var existing = await Db.Applications.FirstOrDefaultAsync(a => a.ProjectId == id && a.UserId == me.Id);
        if (existing is { Status: ApplicationStatus.Aktivs })
            return Validation("Tu jau esi pieteicies šim projektam.");

        // C3 – dalībnieks jau ir komandā šajā projektā (prasība 11.2.).
        if (await Db.TeamMembers.AnyAsync(tm => tm.UserId == me.Id && tm.Team.ProjectId == id))
            return Validation("Tu jau esi komandā šajā projektā.");

        // D4 – izveidot pieteikumu (vai atjaunot iepriekš atsauktu).
        if (existing is not null)
        {
            existing.Status = ApplicationStatus.Aktivs;
            existing.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            Db.Applications.Add(new Application { ProjectId = id, UserId = me.Id });
        }
        await Db.SaveChangesAsync();

        return Ok(new { message = "Pieteikums saglabāts." });
    }

    /// <summary>Pieteikuma atsaukšana līdz termiņa beigām (prasība 5.3.).</summary>
    [HttpDelete("{id:int}/applications")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");

        if (!project.IsOpenForApplications(Today))
            return Validation("Pēc pieteikšanās termiņa beigām pieteikumu atsaukt nav iespējams.");

        var application = await Db.Applications.FirstOrDefaultAsync(a =>
            a.ProjectId == id && a.UserId == me.Id && a.Status == ApplicationStatus.Aktivs);
        if (application is null)
            return Validation("Tev nav aktīva pieteikuma šim projektam.");

        application.Status = ApplicationStatus.Atsaukts;
        await Db.SaveChangesAsync();

        return Ok(new { message = "Pieteikums atsaukts." });
    }

    /// <summary>Pieteikušos dalībnieku saraksts ar prasmēm (organizatoram).</summary>
    [HttpGet("{id:int}/applications")]
    public async Task<IActionResult> Applicants(int id)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await Db.Projects.FirstOrDefaultAsync(p => p.Id == id);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");
        if (project.OrganizerId != me.Id)
            return NoAccess("Pieteikumus var redzēt tikai projekta organizators.");

        var applications = await Db.Applications
            .Where(a => a.ProjectId == id && a.Status == ApplicationStatus.Aktivs)
            .Include(a => a.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(a => a.User).ThenInclude(u => u.Availability)
            .AsSplitQuery()
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        var result = applications.Select(a => new ApplicantDto(
                a.UserId,
                a.User.FullName,
                a.User.PreferredRole,
                MatchingMapper.ToCandidate(a.User).HasCompleteProfile,
                a.User.Skills
                    .OrderByDescending(s => s.Level)
                    .Select(s => new UserSkillDto(s.SkillId, s.Skill.Name, s.Skill.Category, s.Level))
                    .ToList(),
                a.CreatedAt))
            .ToList();

        return Ok(result);
    }

    private IQueryable<Project> ProjectsWithDetails() =>
        Db.Projects
            .Include(p => p.Organizer)
            .Include(p => p.RequiredSkills).ThenInclude(rs => rs.Skill)
            .Include(p => p.Applications)
            .AsSplitQuery();

    private static ProjectDto ToDto(Project p, int currentUserId)
    {
        var today = Today;
        var active = p.Applications.Where(a => a.Status == ApplicationStatus.Aktivs).ToList();
        var mine = p.Applications.FirstOrDefault(a => a.UserId == currentUserId);

        string status = p.Status switch
        {
            ProjectStatus.KomandasApstiprinatas when p.EndDate < today => "Beidzies",
            ProjectStatus.KomandasApstiprinatas => "Komandas apstiprinātas",
            ProjectStatus.Slegts => "Slēgts",
            _ when p.ApplicationDeadline >= today => "Pieteikšanās atvērta",
            _ => "Gaida komandu veidošanu"
        };

        return new ProjectDto(
            p.Id,
            p.Title,
            p.Description,
            p.TeamSize,
            p.ApplicationDeadline,
            p.EndDate,
            status,
            p.IsOpenForApplications(today),
            p.Status == ProjectStatus.Atverts && p.ApplicationDeadline < today,
            p.Organizer.FullName,
            p.RequiredSkills
                .OrderByDescending(rs => rs.MinLevel)
                .Select(rs => new RequiredSkillDto(rs.SkillId, rs.Skill.Name, rs.MinLevel))
                .ToList(),
            p.RequiredRoles,
            active.Count,
            mine?.Status.ToString());
    }
}

using GroupMate.Api.Data;
using GroupMate.Api.Dtos;
using GroupMate.Api.Models;
using GroupMate.Api.Services;
using GroupMate.Core.Matching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Controllers;

/// <summary>
/// Komandu saskaņošanas modulis un komandu pārvaldības modulis
/// (prasības 6.1.–6.7., 7.2., 8.1.; projektējums 5.1.5, 5.1.6, 5.1.7).
/// </summary>
[Route("api/projects/{projectId:int}/teams")]
public class MatchingController : AppControllerBase
{
    public MatchingController(AppDbContext db) : base(db) { }

    /// <summary>Saskaņošanas rezultāts / komandu pārskats.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(int projectId)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await LoadProjectAsync(projectId);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");
        if (project.OrganizerId != me.Id) return NoAccess("Komandas var pārvaldīt tikai projekta organizators.");

        return Ok(await BuildViewAsync(project));
    }

    /// <summary>"Veidot komandas" – saskaņošanas palaišana (prasība 6.1.).</summary>
    [HttpPost("matching")]
    public async Task<IActionResult> RunMatching(int projectId)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await LoadProjectAsync(projectId);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");
        if (project.OrganizerId != me.Id) return NoAccess("Saskaņošanu var palaist tikai projekta organizators.");

        if (project.Status != ProjectStatus.Atverts)
            return Validation("Komandas šim projektam jau ir apstiprinātas.");

        // C1 – pieteikšanās termiņam jābūt beigušamies.
        if (project.ApplicationDeadline >= Today)
            return Validation(
                $"Saskaņošanu var palaist tikai pēc pieteikšanās termiņa beigām ({project.ApplicationDeadline:dd.MM.yyyy}).");

        var applicants = project.Applications
            .Where(a => a.Status == ApplicationStatus.Aktivs)
            .Select(a => MatchingMapper.ToCandidate(a.User))
            .ToList();

        var service = await CreateServiceAsync();
        MatchingResult result;
        try
        {
            result = service.Run(MatchingMapper.ToMatchingProject(project), applicants);
        }
        catch (MatchingException ex)
        {
            // C2 – nepietiek datu komandu veidošanai.
            return Validation(ex.Message);
        }

        // Iepriekšējais (neapstiprinātais) priekšlikums tiek aizstāts ar jauno.
        Db.Teams.RemoveRange(project.Teams.Where(t => !t.IsApproved).ToList());
        await Db.SaveChangesAsync();

        int number = 1;
        foreach (var proposed in result.Teams)
        {
            var team = new Team
            {
                ProjectId = project.Id,
                Name = $"Komanda {number++}",
                Score = proposed.Score.Total,
                SkillScore = proposed.Score.SkillMatch,
                DiversityScore = proposed.Score.Diversity,
                AvailabilityScore = proposed.Score.Availability,
                RoleScore = proposed.Score.Roles,
                Explanation = proposed.Score.Explanation,
                Members = proposed.Members
                    .Select(m => new TeamMember { UserId = m.UserId, Role = m.PreferredRole ?? "" })
                    .ToList()
            };
            Db.Teams.Add(team);
        }
        await Db.SaveChangesAsync();

        Db.ChangeTracker.Clear();
        var reloaded = await LoadProjectAsync(projectId);
        return Ok(await BuildViewAsync(reloaded!));
    }

    /// <summary>Dalībnieka pārvietošana starp komandām (prasība 6.6., projektējums 5.1.6).</summary>
    [HttpPost("move")]
    public async Task<IActionResult> Move(int projectId, MoveMemberRequest req)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await LoadProjectAsync(projectId);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");

        // C1 – tiesības mainīt sastāvu.
        if (project.OrganizerId != me.Id) return NoAccess("Komandas sastāvu var mainīt tikai projekta organizators.");
        if (project.Status != ProjectStatus.Atverts)
            return Validation("Apstiprinātu komandu sastāvu mainīt vairs nevar.");

        var drafts = project.Teams.Where(t => !t.IsApproved).ToList();
        if (drafts.Count == 0) return Validation("Vispirms jāpalaiž komandu saskaņošana.");

        var application = project.Applications.FirstOrDefault(a =>
            a.UserId == req.UserId && a.Status == ApplicationStatus.Aktivs);
        if (application is null) return Validation("Dalībnieks nav pieteicies šim projektam.");

        var sourceTeam = drafts.FirstOrDefault(t => t.Members.Any(m => m.UserId == req.UserId));
        var targetTeam = req.TargetTeamId is null ? null : drafts.FirstOrDefault(t => t.Id == req.TargetTeamId);

        if (req.TargetTeamId is not null && targetTeam is null)
            return Validation("Mērķa komanda nav atrasta.");
        if (sourceTeam is not null && targetTeam is not null && sourceTeam.Id == targetTeam.Id)
            return Validation("Dalībnieks jau ir šajā komandā.");
        if (sourceTeam is null && targetTeam is null)
            return Validation("Dalībnieks jau nav nevienā komandā.");

        // C2 – komandas lielums pēc izmaiņām 2–10 (prasība 11.1.).
        if (sourceTeam is not null && sourceTeam.Members.Count - 1 < 2)
            return Validation($"Nevar izņemt dalībnieku: komandā „{sourceTeam.Name}” paliktu mazāk par 2 dalībniekiem.");
        if (targetTeam is not null && targetTeam.Members.Count + 1 > 10)
            return Validation($"Nevar pievienot dalībnieku: komandā „{targetTeam.Name}” būtu vairāk par 10 dalībniekiem.");

        // C3 – dalībnieks vienā projektā var būt tikai vienā komandā (prasība 11.2.):
        // tāpēc pārvietošana vienmēr vispirms izņem dalībnieku no iepriekšējās komandas.
        var service = await CreateServiceAsync();
        if (sourceTeam is not null)
        {
            var member = sourceTeam.Members.First(m => m.UserId == req.UserId);
            sourceTeam.Members.Remove(member);
            Db.TeamMembers.Remove(member);
            MatchingMapper.ApplyScore(sourceTeam, project, service);
        }

        if (targetTeam is not null)
        {
            var newMember = new TeamMember
            {
                TeamId = targetTeam.Id,
                UserId = application.UserId,
                User = application.User,
                Role = application.User.PreferredRole ?? ""
            };
            targetTeam.Members.Add(newMember);
            MatchingMapper.ApplyScore(targetTeam, project, service);
        }

        // D4 – saglabāt izmaiņas un pārrēķināto novērtējumu.
        await Db.SaveChangesAsync();

        Db.ChangeTracker.Clear();
        var reloaded = await LoadProjectAsync(projectId);
        return Ok(await BuildViewAsync(reloaded!));
    }

    /// <summary>Dalībnieka lomas maiņa komandā (prasība 7.2., saskarne "Komandu rediģēšana").</summary>
    [HttpPut("{teamId:int}/members/{userId:int}/role")]
    public async Task<IActionResult> ChangeRole(int projectId, int teamId, int userId, ChangeRoleRequest req)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await LoadProjectAsync(projectId);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");
        if (project.OrganizerId != me.Id) return NoAccess("Lomas var mainīt tikai projekta organizators.");
        if (!SeedData.AllRoles.Contains(req.Role)) return Validation("Nezināma loma.");

        var team = project.Teams.FirstOrDefault(t => t.Id == teamId);
        var member = team?.Members.FirstOrDefault(m => m.UserId == userId);
        if (team is null || member is null) return NotFoundMessage("Komandas dalībnieks nav atrasts.");

        member.Role = req.Role;
        MatchingMapper.ApplyScore(team, project, await CreateServiceAsync());
        await Db.SaveChangesAsync();

        Db.ChangeTracker.Clear();
        var reloaded = await LoadProjectAsync(projectId);
        return Ok(await BuildViewAsync(reloaded!));
    }

    /// <summary>Komandu apstiprināšana un paziņojumu izveide (prasības 6.7., 8.1.).</summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve(int projectId)
    {
        var me = await CurrentUserAsync();
        if (me is null) return NotLoggedIn();

        var project = await LoadProjectAsync(projectId);
        if (project is null) return NotFoundMessage("Projekts nav atrasts.");
        if (project.OrganizerId != me.Id) return NoAccess("Komandas var apstiprināt tikai projekta organizators.");
        if (project.Status != ProjectStatus.Atverts) return Validation("Komandas jau ir apstiprinātas.");

        var drafts = project.Teams.Where(t => !t.IsApproved).ToList();
        if (drafts.Count == 0) return Validation("Nav komandu, ko apstiprināt. Vispirms palaid saskaņošanu.");
        if (drafts.Any(t => t.Members.Count < 2 || t.Members.Count > 10))
            return Validation("Katrā komandā jābūt no 2 līdz 10 dalībniekiem.");

        var now = DateTime.UtcNow;
        foreach (var team in drafts)
        {
            team.IsApproved = true;
            team.ApprovedAt = now;

            // Paziņojumu modulis: sistēmas paziņojums katram dalībniekam.
            // E-pasta nosūtīšana caur SMTP prototipā tiek tikai simulēta (statuss saglabāts datubāzē).
            foreach (var m in team.Members)
                Db.Notifications.Add(new Notification
                {
                    UserId = m.UserId,
                    Title = "Komanda apstiprināta",
                    Message = $"Tava komanda „{team.Name}” projektā „{project.Title}” ir apstiprināta. " +
                              $"Tava loma: {m.Role}. Komandas biedru kontaktinformācija tagad ir redzama sadaļā „Manas komandas”.",
                    CreatedAt = now
                });
        }

        project.Status = ProjectStatus.KomandasApstiprinatas;
        await Db.SaveChangesAsync();

        Db.ChangeTracker.Clear();
        var reloaded = await LoadProjectAsync(projectId);
        return Ok(await BuildViewAsync(reloaded!));
    }

    private async Task<Project?> LoadProjectAsync(int projectId) =>
        await Db.Projects
            .Include(p => p.RequiredSkills).ThenInclude(rs => rs.Skill)
            .Include(p => p.Applications).ThenInclude(a => a.User).ThenInclude(u => u.Skills).ThenInclude(s => s.Skill)
            .Include(p => p.Applications).ThenInclude(a => a.User).ThenInclude(u => u.Availability)
            .Include(p => p.Teams).ThenInclude(t => t.Members).ThenInclude(m => m.User).ThenInclude(u => u.Skills)
                .ThenInclude(s => s.Skill)
            .Include(p => p.Teams).ThenInclude(t => t.Members).ThenInclude(m => m.User).ThenInclude(u => u.Availability)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == projectId);

    private async Task<MatchingService> CreateServiceAsync()
    {
        var names = await Db.Skills.ToDictionaryAsync(s => s.Id, s => s.Name);
        return new MatchingService(names);
    }

    private async Task<MatchingViewDto> BuildViewAsync(Project project)
    {
        var service = await CreateServiceAsync();
        var teams = project.Teams.OrderBy(t => t.Id).ToList();
        var inTeams = teams.SelectMany(t => t.Members).Select(m => m.UserId).ToHashSet();

        var activeApplicants = project.Applications
            .Where(a => a.Status == ApplicationStatus.Aktivs)
            .Select(a => a.User)
            .ToList();

        var unassigned = new List<UnassignedDto>();
        var gapReport = new List<string>();

        if (teams.Count > 0)
        {
            foreach (var u in activeApplicants.Where(u => !inTeams.Contains(u.Id)))
            {
                bool complete = MatchingMapper.ToCandidate(u).HasCompleteProfile;
                unassigned.Add(new UnassignedDto(
                    u.Id,
                    u.FullName,
                    u.PreferredRole,
                    complete
                        ? "Nav iekļauts nevienā komandā (visas komandas ir pilnas vai organizators to izņēma)."
                        : "Profils nav pilnībā aizpildīts (trūkst prasmju, pieejamā laika vai vēlamās lomas)."));
            }

            var eligible = activeApplicants
                .Select(u => MatchingMapper.ToCandidate(u))
                .Where(c => c.HasCompleteProfile)
                .ToList();
            gapReport = service.BuildSkillGapReport(MatchingMapper.ToMatchingProject(project), eligible, teams.Count);
        }

        bool approved = project.Status != ProjectStatus.Atverts;

        return new MatchingViewDto(
            project.Id,
            project.Title,
            project.TeamSize,
            approved,
            teams.Select(t => new TeamDto(
                    t.Id,
                    t.Name,
                    t.Score,
                    t.SkillScore,
                    t.DiversityScore,
                    t.AvailabilityScore,
                    t.RoleScore,
                    t.Explanation,
                    t.IsApproved,
                    t.Members
                        .OrderBy(m => m.User.FirstName)
                        .Select(m => new TeamMemberDto(
                            m.UserId,
                            m.User.FullName,
                            m.Role,
                            m.User.Email, // organizators redz e-pastus vienmēr
                            m.User.Skills
                                .OrderByDescending(s => s.Level)
                                .Select(s => new MemberSkillDto(s.Skill.Name, s.Level))
                                .ToList()))
                        .ToList()))
                .ToList(),
            unassigned,
            gapReport);
    }
}

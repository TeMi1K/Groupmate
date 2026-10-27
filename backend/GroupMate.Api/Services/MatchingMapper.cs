using GroupMate.Api.Models;
using GroupMate.Core.Matching;

namespace GroupMate.Api.Services;

/// <summary>Pārveido datubāzes entītijas saskaņošanas moduļa ievaddatos.</summary>
public static class MatchingMapper
{
    /// <param name="roleOverride">Komandā piešķirtā loma (ja atšķiras no vēlamās).</param>
    public static Candidate ToCandidate(User u, string? roleOverride = null) => new()
    {
        UserId = u.Id,
        FullName = u.FullName,
        PreferredRole = string.IsNullOrWhiteSpace(roleOverride) ? u.PreferredRole : roleOverride,
        Skills = u.Skills.ToDictionary(s => s.SkillId, s => s.Level),
        Availability = u.Availability
            .Select(a => new AvailabilitySlot(a.DayOfWeek, a.StartHour, a.EndHour))
            .ToList()
    };

    public static MatchingProject ToMatchingProject(Project p) => new()
    {
        TeamSize = p.TeamSize,
        RequiredRoles = p.RequiredRoles.ToList(),
        RequiredSkills = p.RequiredSkills
            .Select(rs => new SkillRequirement(rs.SkillId, rs.Skill.Name, rs.MinLevel))
            .ToList()
    };

    /// <summary>Pārrēķina un ieraksta komandas saderības novērtējumu (prasība 6.6.).</summary>
    public static void ApplyScore(Team team, Project project, MatchingService service)
    {
        var members = team.Members.Select(m => ToCandidate(m.User, m.Role)).ToList();
        var score = service.Score(members, ToMatchingProject(project));
        team.Score = score.Total;
        team.SkillScore = score.SkillMatch;
        team.DiversityScore = score.Diversity;
        team.AvailabilityScore = score.Availability;
        team.RoleScore = score.Roles;
        team.Explanation = score.Explanation;
    }
}

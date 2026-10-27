using GroupMate.Core.Matching;
using Xunit;

namespace GroupMate.Tests;

/// <summary>Komandu saskaņošanas moduļa vienībtesti (nefunkcionālā prasība 1.5.).</summary>
public class MatchingServiceTests
{
    private const int CSharp = 1, React = 2, UiUx = 3, Testing = 4, Management = 5;

    private static readonly MatchingProject Project = new()
    {
        TeamSize = 3,
        RequiredSkills =
        {
            new SkillRequirement(CSharp, "C#", 3),
            new SkillRequirement(UiUx, "UI/UX", 3),
            new SkillRequirement(Testing, "Testēšana", 3)
        },
        RequiredRoles = { "Izstrādātājs", "Dizainers", "Testētājs" }
    };

    private static Candidate Person(int id, string role, params (int skill, int level)[] skills) => new()
    {
        UserId = id,
        FullName = $"Dalībnieks {id}",
        PreferredRole = role,
        Skills = skills.ToDictionary(s => s.skill, s => s.level),
        Availability = { new AvailabilitySlot(1, 18, 21), new AvailabilitySlot(3, 18, 21) }
    };

    private static List<Candidate> SixBalancedCandidates() => new()
    {
        Person(1, "Izstrādātājs", (CSharp, 5)),
        Person(2, "Izstrādātājs", (CSharp, 4), (React, 3)),
        Person(3, "Dizainers", (UiUx, 5)),
        Person(4, "Dizainers", (UiUx, 4)),
        Person(5, "Testētājs", (Testing, 4)),
        Person(6, "Testētājs", (Testing, 3))
    };

    [Fact]
    public void Run_CreatesExpectedNumberOfTeams_WithCorrectSize()
    {
        var result = new MatchingService().Run(Project, SixBalancedCandidates());

        Assert.Equal(2, result.Teams.Count);
        Assert.All(result.Teams, t => Assert.Equal(3, t.Members.Count));
        Assert.Empty(result.Unassigned);
    }

    [Fact]
    public void Run_EachParticipantIsInAtMostOneTeam()
    {
        var result = new MatchingService().Run(Project, SixBalancedCandidates());

        var ids = result.Teams.SelectMany(t => t.Members).Select(m => m.UserId).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Run_BuildsComplementaryTeams_WithoutRoleDuplication()
    {
        var result = new MatchingService().Run(Project, SixBalancedCandidates());

        // Katrā komandā jābūt izstrādātājam, dizainerim un testētājam – nevis, piem., diviem izstrādātājiem.
        Assert.All(result.Teams, t =>
            Assert.Equal(3, t.Members.Select(m => m.PreferredRole).Distinct().Count()));
        Assert.All(result.Teams, t => Assert.Equal(100, t.Score.Total));
    }

    [Fact]
    public void Score_IsAlwaysBetween0And100()
    {
        var service = new MatchingService();
        var weak = new List<Candidate>
        {
            Person(1, "Izstrādātājs", (React, 1)),
            new() { UserId = 2, PreferredRole = "Izstrādātājs", Skills = { [React] = 1 },
                    Availability = { new AvailabilitySlot(7, 8, 9) } }
        };

        var weakScore = service.Score(weak, Project);
        var strongScore = service.Score(SixBalancedCandidates().Where(c => c.UserId is 1 or 3 or 5).ToList(), Project);

        Assert.InRange(weakScore.Total, 0, 100);
        Assert.InRange(strongScore.Total, 0, 100);
        Assert.True(strongScore.Total > weakScore.Total);
        Assert.Equal(
            strongScore.Total,
            strongScore.SkillMatch + strongScore.Diversity + strongScore.Availability + strongScore.Roles);
    }

    [Fact]
    public void Run_ParticipantWithIncompleteProfile_IsShownAsUnassigned()
    {
        var candidates = SixBalancedCandidates();
        candidates.Add(new Candidate { UserId = 99, FullName = "Bez profila" });

        var result = new MatchingService().Run(Project, candidates);

        var unassigned = Assert.Single(result.Unassigned);
        Assert.Equal(99, unassigned.Candidate.UserId);
        Assert.Contains("Profils", unassigned.Reason);
    }

    [Fact]
    public void Run_ExtraParticipants_AreShownAsUnassigned()
    {
        var candidates = SixBalancedCandidates();
        candidates.Add(Person(7, "Izstrādātājs", (CSharp, 2)));

        var result = new MatchingService().Run(Project, candidates);

        Assert.Equal(2, result.Teams.Count);
        var unassigned = Assert.Single(result.Unassigned);
        Assert.Equal(7, unassigned.Candidate.UserId); // vājākais dalībnieks paliek ārpus komandām
    }

    [Fact]
    public void Run_NotEnoughParticipants_Throws()
    {
        var candidates = SixBalancedCandidates().Take(2).ToList();

        var ex = Assert.Throws<MatchingException>(() => new MatchingService().Run(Project, candidates));
        Assert.Contains("Nepietiek", ex.Message);
    }

    [Fact]
    public void Run_InvalidTeamSize_Throws()
    {
        var project = new MatchingProject { TeamSize = 11 };

        Assert.Throws<MatchingException>(() => new MatchingService().Run(project, SixBalancedCandidates()));
    }

    [Fact]
    public void SkillGapReport_ListsMissingSkillsAndRoles()
    {
        var project = new MatchingProject
        {
            TeamSize = 3,
            RequiredSkills = { new SkillRequirement(Management, "Projektu vadība", 3) },
            RequiredRoles = { "Projekta vadītājs" }
        };

        var result = new MatchingService().Run(project, SixBalancedCandidates());

        Assert.Contains(result.SkillGapReport, r => r.Contains("Projektu vadība"));
        Assert.Contains(result.SkillGapReport, r => r.Contains("Projekta vadītājs"));
    }

    [Fact]
    public void CommonHours_CountsOnlyHoursWhenEveryoneIsAvailable()
    {
        var a = new Candidate { Availability = { new AvailabilitySlot(1, 18, 22) } };
        var b = new Candidate { Availability = { new AvailabilitySlot(1, 20, 23), new AvailabilitySlot(2, 18, 20) } };

        Assert.Equal(2, MatchingService.CommonHours(new[] { a, b }));
    }
}

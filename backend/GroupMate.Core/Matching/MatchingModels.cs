namespace GroupMate.Core.Matching;

/// <summary>Projekta prasība: prasme un minimālais nepieciešamais līmenis (1–5).</summary>
public record SkillRequirement(int SkillId, string SkillName, int MinLevel);

/// <summary>Dalībnieka pieejamais laiks vienā nedēļas dienā (stundas, piem. 18–21).</summary>
public record AvailabilitySlot(int DayOfWeek, int StartHour, int EndHour);

/// <summary>Saskaņošanai nepieciešamie dalībnieka profila dati.</summary>
public class Candidate
{
    public int UserId { get; init; }
    public string FullName { get; init; } = "";
    public string? PreferredRole { get; init; }

    /// <summary>Prasmes: SkillId → līmenis 1–5.</summary>
    public Dictionary<int, int> Skills { get; init; } = new();

    public List<AvailabilitySlot> Availability { get; init; } = new();

    /// <summary>Vai profils ir pietiekami aizpildīts saskaņošanai.</summary>
    public bool HasCompleteProfile =>
        Skills.Count > 0 && Availability.Count > 0 && !string.IsNullOrWhiteSpace(PreferredRole);
}

/// <summary>Projekta dati, kas vajadzīgi saskaņošanai.</summary>
public class MatchingProject
{
    public int TeamSize { get; init; }
    public List<SkillRequirement> RequiredSkills { get; init; } = new();
    public List<string> RequiredRoles { get; init; } = new();
}

/// <summary>Saderības novērtējums 0–100 un tā sadalījums pa 4 kritērijiem.</summary>
public class TeamScore
{
    /// <summary>Kopējais novērtējums 0–100.</summary>
    public int Total { get; init; }

    /// <summary>Prasmju atbilstība projekta prasībām (0–40).</summary>
    public int SkillMatch { get; init; }

    /// <summary>Prasmju dažādība komandā (0–20).</summary>
    public int Diversity { get; init; }

    /// <summary>Pieejamā laika sakritība (0–20).</summary>
    public int Availability { get; init; }

    /// <summary>Lomu sadalījums bez dublēšanās (0–20).</summary>
    public int Roles { get; init; }

    /// <summary>Teksta skaidrojums pa kritērijiem (organizatoram).</summary>
    public List<string> Explanation { get; init; } = new();

    /// <summary>Precīza (nenoapaļota) vērtība, ko izmanto optimizācijā.</summary>
    public double Raw { get; init; }
}

public class ProposedTeam
{
    public List<Candidate> Members { get; init; } = new();
    public TeamScore Score { get; set; } = new();
}

public class UnassignedCandidate
{
    public Candidate Candidate { get; init; } = null!;
    public string Reason { get; init; } = "";
}

public class MatchingResult
{
    public List<ProposedTeam> Teams { get; init; } = new();
    public List<UnassignedCandidate> Unassigned { get; init; } = new();

    /// <summary>Prasmju trūkumu atskaite – kuras projekta prasības nav nosegtas.</summary>
    public List<string> SkillGapReport { get; init; } = new();
}

/// <summary>Kļūda, ko izraisa nepietiekami dati saskaņošanai (5.1.5 D2).</summary>
public class MatchingException : Exception
{
    public MatchingException(string message) : base(message) { }
}

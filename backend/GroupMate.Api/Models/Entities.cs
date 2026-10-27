namespace GroupMate.Api.Models;

// Datubāzes tabulas atbilst projektējuma 2.2. nodaļai (datu dekompozīcija).

public enum UserRole
{
    Dalibnieks,
    Organizators,
    Administrators
}

public enum ProjectStatus
{
    /// <summary>Pieņem pieteikumus vai gaida saskaņošanu.</summary>
    Atverts,

    /// <summary>Komandas ir apstiprinātas.</summary>
    KomandasApstiprinatas,

    /// <summary>Projekts slēgts pēc beigu datuma.</summary>
    Slegts
}

public enum ApplicationStatus
{
    Aktivs,
    Atsaukts
}

/// <summary>Users – lietotāja konts, vārds, uzvārds, e-pasts, autentifikācijas un lomas dati.</summary>
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";

    /// <summary>Prototipā autentifikācija nav realizēta, lauks paredzēts paroles jaucējvērtībai.</summary>
    public string PasswordHash { get; set; } = "";

    public UserRole Role { get; set; } = UserRole.Dalibnieks;
    public bool IsBlocked { get; set; }
    public bool ConsentGiven { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Profila dati (3. prasību grupa).
    public string? PreferredRole { get; set; }
    public List<string> Interests { get; set; } = new();

    public List<UserSkill> Skills { get; set; } = new();
    public List<Availability> Availability { get; set; } = new();

    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>Skills – prasmju katalogs.</summary>
public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
}

/// <summary>UserSkills – lietotāja prasme ar līmeni 1–5.</summary>
public class UserSkill
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public int Level { get; set; }
}

/// <summary>Availability – pieejamais laiks pa nedēļas dienām (1 = pirmdiena … 7 = svētdiena) un stundām.</summary>
public class Availability
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int DayOfWeek { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
}

/// <summary>Projects – projekta pamatinformācija un termiņi.</summary>
public class Project
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int TeamSize { get; set; }
    public DateOnly ApplicationDeadline { get; set; }
    public DateOnly EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Atverts;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Nepieciešamās lomas komandā (piem., Izstrādātājs, Dizainers).</summary>
    public List<string> RequiredRoles { get; set; } = new();

    public int OrganizerId { get; set; }
    public User Organizer { get; set; } = null!;

    public List<ProjectSkill> RequiredSkills { get; set; } = new();
    public List<Application> Applications { get; set; } = new();
    public List<Team> Teams { get; set; } = new();

    /// <summary>Pieteikšanās iespējama līdz termiņa dienai ieskaitot.</summary>
    public bool IsOpenForApplications(DateOnly today) =>
        Status == ProjectStatus.Atverts && today <= ApplicationDeadline;
}

/// <summary>ProjectSkills – projektam nepieciešamās prasmes un minimālais līmenis.</summary>
public class ProjectSkill
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int SkillId { get; set; }
    public Skill Skill { get; set; } = null!;
    public int MinLevel { get; set; }
}

/// <summary>Applications – dalībnieku pieteikumi projektiem.</summary>
public class Application
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Aktivs;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Teams – izveidotās komandas un saderības novērtējums.</summary>
public class Team
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Name { get; set; } = "";

    public int Score { get; set; }
    public int SkillScore { get; set; }
    public int DiversityScore { get; set; }
    public int AvailabilityScore { get; set; }
    public int RoleScore { get; set; }

    /// <summary>Saderības skaidrojums pa kritērijiem.</summary>
    public List<string> Explanation { get; set; } = new();

    /// <summary>false – saskaņošanas priekšlikums (melnraksts); true – organizatora apstiprināta komanda.</summary>
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }

    public List<TeamMember> Members { get; set; } = new();
}

/// <summary>TeamMembers – komandas dalībnieki un piešķirtās lomas.</summary>
public class TeamMember
{
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Role { get; set; } = "";
}

/// <summary>Feedback – atsauksmes pēc projekta (prototipā tikai tabula, saskarne vizuāla).</summary>
public class Feedback
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Notifications – sistēmas paziņojumi un e-pasta nosūtīšanas statuss.</summary>
public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public bool IsRead { get; set; }

    /// <summary>Prototipā SMTP nav pieslēgts, tāpēc e-pasts tiek tikai simulēts.</summary>
    public string EmailStatus { get; set; } = "Simulēts (SMTP prototipā nav pieslēgts)";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

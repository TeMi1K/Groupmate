namespace GroupMate.Api.Dtos;

// ---------- Kopīgie ----------

public record SkillDto(int Id, string Name, string Category);

public record DemoUserDto(int Id, string FullName, string Email, string Role, string? PreferredRole);

// ---------- Profils ----------

public record UserSkillDto(int SkillId, string Name, string Category, int Level);

public record AvailabilityDto(int DayOfWeek, int StartHour, int EndHour);

public record ProfileDto(
    int Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    string? PreferredRole,
    List<string> Interests,
    List<UserSkillDto> Skills,
    List<AvailabilityDto> Availability);

public record SkillLevelRequest(int SkillId, int Level);

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PreferredRole,
    List<string>? Interests,
    List<SkillLevelRequest>? Skills,
    List<AvailabilityDto>? Availability);

// ---------- Projekti ----------

public record RequiredSkillDto(int SkillId, string Name, int MinLevel);

public record ProjectDto(
    int Id,
    string Title,
    string Description,
    int TeamSize,
    DateOnly ApplicationDeadline,
    DateOnly EndDate,
    string Status,
    bool IsOpenForApplications,
    bool CanRunMatching,
    string OrganizerName,
    List<RequiredSkillDto> RequiredSkills,
    List<string> RequiredRoles,
    int ApplicantCount,
    string? MyApplicationStatus);

public record RequiredSkillRequest(int SkillId, int MinLevel);

public record CreateProjectRequest(
    string Title,
    string Description,
    int TeamSize,
    DateOnly ApplicationDeadline,
    DateOnly EndDate,
    List<RequiredSkillRequest>? RequiredSkills,
    List<string>? RequiredRoles);

public record ApplicantDto(
    int UserId,
    string FullName,
    string? PreferredRole,
    bool ProfileComplete,
    List<UserSkillDto> Skills,
    DateTime AppliedAt);

// ---------- Saskaņošana un komandas ----------

public record MemberSkillDto(string Name, int Level);

public record TeamMemberDto(int UserId, string FullName, string Role, string? Email, List<MemberSkillDto> Skills);

public record TeamDto(
    int Id,
    string Name,
    int Score,
    int SkillScore,
    int DiversityScore,
    int AvailabilityScore,
    int RoleScore,
    List<string> Explanation,
    bool IsApproved,
    List<TeamMemberDto> Members);

public record UnassignedDto(int UserId, string FullName, string? PreferredRole, string Reason);

public record MatchingViewDto(
    int ProjectId,
    string ProjectTitle,
    int TeamSize,
    bool IsApproved,
    List<TeamDto> Teams,
    List<UnassignedDto> Unassigned,
    List<string> SkillGapReport);

/// <summary>TargetTeamId = null nozīmē izņemt dalībnieku no komandas (uz neiekļauto sarakstu).</summary>
public record MoveMemberRequest(int UserId, int? TargetTeamId);

public record ChangeRoleRequest(string Role);

// ---------- Dalībnieka komandas ----------

public record MyTeamDto(
    int TeamId,
    string TeamName,
    int ProjectId,
    string ProjectTitle,
    DateOnly EndDate,
    bool ProjectEnded,
    string MyRole,
    int Score,
    DateTime? ApprovedAt,
    List<TeamMemberDto> Members);

public record NotificationDto(int Id, string Title, string Message, bool IsRead, string EmailStatus, DateTime CreatedAt);

// ---------- Administrēšana ----------

public record AdminUserDto(int Id, string FullName, string Email, string Role, bool IsBlocked, DateTime CreatedAt);

public record StatsDto(int Users, int Participants, int Organizers, int Projects, int OpenProjects, int ApprovedTeams, int Applications);

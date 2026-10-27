using GroupMate.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GroupMate.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<UserSkill> UserSkills => Set<UserSkill>();
    public DbSet<Availability> Availability => Set<Availability>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectSkill> ProjectSkills => Set<ProjectSkill>();
    public DbSet<Application> Applications => Set<Application>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasConversion<string>();
            e.Ignore(u => u.FullName);
        });

        b.Entity<Skill>().HasIndex(s => s.Name).IsUnique();

        b.Entity<UserSkill>(e =>
        {
            e.HasKey(us => new { us.UserId, us.SkillId });
            e.HasOne(us => us.User).WithMany(u => u.Skills).HasForeignKey(us => us.UserId);
            e.HasOne(us => us.Skill).WithMany().HasForeignKey(us => us.SkillId);
            e.ToTable(t => t.HasCheckConstraint("CK_UserSkills_Level", "\"Level\" BETWEEN 1 AND 5"));
        });

        b.Entity<Availability>(e =>
        {
            e.HasOne(a => a.User).WithMany(u => u.Availability).HasForeignKey(a => a.UserId);
        });

        b.Entity<Project>(e =>
        {
            e.Property(p => p.Status).HasConversion<string>();
            e.HasOne(p => p.Organizer).WithMany().HasForeignKey(p => p.OrganizerId)
                .OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t => t.HasCheckConstraint("CK_Projects_TeamSize", "\"TeamSize\" BETWEEN 2 AND 10"));
        });

        b.Entity<ProjectSkill>(e =>
        {
            e.HasOne(ps => ps.Project).WithMany(p => p.RequiredSkills).HasForeignKey(ps => ps.ProjectId);
            e.HasOne(ps => ps.Skill).WithMany().HasForeignKey(ps => ps.SkillId);
        });

        b.Entity<Application>(e =>
        {
            // Vienam projektam dalībnieks var pieteikties tikai vienu reizi (prasība 5.2).
            e.HasIndex(a => new { a.ProjectId, a.UserId }).IsUnique();
            e.Property(a => a.Status).HasConversion<string>();
            e.HasOne(a => a.Project).WithMany(p => p.Applications).HasForeignKey(a => a.ProjectId);
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId);
        });

        b.Entity<Team>(e =>
        {
            e.HasOne(t => t.Project).WithMany(p => p.Teams).HasForeignKey(t => t.ProjectId);
        });

        b.Entity<TeamMember>(e =>
        {
            e.HasKey(tm => new { tm.TeamId, tm.UserId });
            e.HasOne(tm => tm.Team).WithMany(t => t.Members).HasForeignKey(tm => tm.TeamId);
            e.HasOne(tm => tm.User).WithMany().HasForeignKey(tm => tm.UserId);
        });

        b.Entity<Feedback>(e =>
        {
            e.HasIndex(f => new { f.ProjectId, f.UserId }).IsUnique();
        });

        b.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId);
        });
    }
}

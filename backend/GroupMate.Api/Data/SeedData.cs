using GroupMate.Api.Models;
using GroupMate.Api.Services;
using GroupMate.Core.Matching;

namespace GroupMate.Api.Data;

/// <summary>
/// Demonstrācijas dati prototipam. Visi vārdi un e-pasti ir izdomāti.
/// Termiņi tiek rēķināti attiecībā pret šodienu, lai demonstrācija strādātu jebkurā dienā.
/// </summary>
public static class SeedData
{
    public const string RoleDeveloper = "Izstrādātājs";
    public const string RoleDesigner = "Dizainers";
    public const string RoleManager = "Projekta vadītājs";
    public const string RoleTester = "Testētājs";
    public const string RoleAnalyst = "Datu analītiķis";

    public static readonly string[] AllRoles = { RoleDeveloper, RoleDesigner, RoleManager, RoleTester, RoleAnalyst };

    public static void Seed(AppDbContext db)
    {
        if (db.Users.Any()) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // ---------- Prasmju katalogs ----------
        var skills = new Dictionary<string, Skill>();
        void AddSkill(string name, string category) => skills[name] = new Skill { Name = name, Category = category };

        AddSkill("C#", "Programmēšana");
        AddSkill("JavaScript/TypeScript", "Programmēšana");
        AddSkill("React", "Programmēšana");
        AddSkill("Python", "Programmēšana");
        AddSkill("Java", "Programmēšana");
        AddSkill("SQL / datubāzes", "Programmēšana");
        AddSkill("Docker / DevOps", "Programmēšana");
        AddSkill("UI/UX dizains", "Dizains");
        AddSkill("Figma", "Dizains");
        AddSkill("Grafiskais dizains", "Dizains");
        AddSkill("Projektu vadība", "Vadība");
        AddSkill("Prezentēšana", "Vadība");
        AddSkill("Testēšana (QA)", "Testēšana");
        AddSkill("Datu analīze", "Dati");
        db.Skills.AddRange(skills.Values);

        // ---------- Lietotāji ----------
        User NewUser(string first, string last, UserRole role, string? preferredRole = null, params string[] interests) =>
            new()
            {
                FirstName = first,
                LastName = last,
                Email = $"{Translit(first)}.{Translit(last)}@example.lv",
                PasswordHash = "prototype-no-auth",
                Role = role,
                ConsentGiven = true,
                PreferredRole = preferredRole,
                Interests = interests.ToList()
            };

        void Sk(User u, string name, int level) =>
            u.Skills.Add(new UserSkill { User = u, Skill = skills[name], Level = level });

        void Avail(User u, int day, int from, int to) =>
            u.Availability.Add(new Availability { User = u, DayOfWeek = day, StartHour = from, EndHour = to });

        var organizer = NewUser("Laura", "Kalniņa", UserRole.Organizators);
        var admin = NewUser("Mārtiņš", "Liepa", UserRole.Administrators);

        var anna = NewUser("Anna", "Bērziņa", UserRole.Dalibnieks, RoleDeveloper, "Tīmekļa izstrāde", "Spēles");
        Sk(anna, "C#", 4); Sk(anna, "React", 3); Sk(anna, "SQL / datubāzes", 3);
        Avail(anna, 1, 18, 21); Avail(anna, 3, 18, 21); Avail(anna, 6, 10, 14);

        var roberts = NewUser("Roberts", "Kalns", UserRole.Dalibnieks, RoleDeveloper, "Mākoņtehnoloģijas");
        Sk(roberts, "C#", 5); Sk(roberts, "SQL / datubāzes", 4); Sk(roberts, "Docker / DevOps", 3);
        Avail(roberts, 2, 17, 21); Avail(roberts, 4, 17, 21);

        var elina = NewUser("Elīna", "Ozola", UserRole.Dalibnieks, RoleDesigner, "Dizains", "Fotogrāfija");
        Sk(elina, "UI/UX dizains", 5); Sk(elina, "Figma", 5); Sk(elina, "Grafiskais dizains", 3);
        Avail(elina, 1, 17, 21); Avail(elina, 3, 17, 21); Avail(elina, 6, 11, 15);

        var kristaps = NewUser("Kristaps", "Liepiņš", UserRole.Dalibnieks, RoleManager, "Jaunuzņēmumi");
        Sk(kristaps, "Projektu vadība", 4); Sk(kristaps, "Prezentēšana", 4); Sk(kristaps, "SQL / datubāzes", 2);
        Avail(kristaps, 2, 18, 22); Avail(kristaps, 4, 18, 22); Avail(kristaps, 6, 10, 13);

        var marta = NewUser("Marta", "Krūmiņa", UserRole.Dalibnieks, RoleTester, "Kvalitāte", "Automatizācija");
        Sk(marta, "Testēšana (QA)", 4); Sk(marta, "SQL / datubāzes", 3); Sk(marta, "Python", 2);
        Avail(marta, 1, 18, 21); Avail(marta, 3, 18, 21); Avail(marta, 5, 15, 18);

        var davis = NewUser("Dāvis", "Zariņš", UserRole.Dalibnieks, RoleDeveloper, "Frontend", "Mūzika");
        Sk(davis, "JavaScript/TypeScript", 4); Sk(davis, "React", 5); Sk(davis, "UI/UX dizains", 2);
        Avail(davis, 2, 18, 21); Avail(davis, 4, 18, 21); Avail(davis, 7, 12, 16);

        var laima = NewUser("Laima", "Vītola", UserRole.Dalibnieks, RoleDesigner, "Ilustrācija");
        Sk(laima, "Figma", 4); Sk(laima, "UI/UX dizains", 3); Sk(laima, "Grafiskais dizains", 4);
        Avail(laima, 2, 17, 20); Avail(laima, 4, 17, 20);

        var edgars = NewUser("Edgars", "Siliņš", UserRole.Dalibnieks, RoleDeveloper, "Mākslīgais intelekts");
        Sk(edgars, "Python", 4); Sk(edgars, "Datu analīze", 4); Sk(edgars, "SQL / datubāzes", 4);
        Avail(edgars, 1, 19, 22); Avail(edgars, 3, 19, 22);

        var ieva = NewUser("Ieva", "Pētersone", UserRole.Dalibnieks, RoleManager, "Pasākumu organizēšana");
        Sk(ieva, "Projektu vadība", 3); Sk(ieva, "Prezentēšana", 5); Sk(ieva, "UI/UX dizains", 2);
        Avail(ieva, 1, 18, 21); Avail(ieva, 3, 18, 21);

        var toms = NewUser("Toms", "Celmiņš", UserRole.Dalibnieks, RoleTester, "Spēles");
        Sk(toms, "Testēšana (QA)", 3); Sk(toms, "JavaScript/TypeScript", 3); Sk(toms, "Docker / DevOps", 2);
        Avail(toms, 2, 18, 21); Avail(toms, 4, 18, 21);

        var agnese = NewUser("Agnese", "Lāce", UserRole.Dalibnieks, RoleDeveloper, "Tīmekļa izstrāde");
        Sk(agnese, "C#", 3); Sk(agnese, "JavaScript/TypeScript", 3); Sk(agnese, "React", 3);
        Avail(agnese, 1, 17, 20); Avail(agnese, 3, 17, 20); Avail(agnese, 5, 14, 17);

        var rihards = NewUser("Rihards", "Eglītis", UserRole.Dalibnieks, RoleAnalyst, "Statistika");
        Sk(rihards, "Datu analīze", 5); Sk(rihards, "Python", 4);
        Avail(rihards, 2, 16, 20); Avail(rihards, 5, 10, 14);

        var sanita = NewUser("Sanita", "Grīnberga", UserRole.Dalibnieks, RoleDesigner, "Lietojamība");
        Sk(sanita, "UI/UX dizains", 4); Sk(sanita, "Figma", 3); Sk(sanita, "Prezentēšana", 3);
        Avail(sanita, 2, 18, 21); Avail(sanita, 4, 18, 21); Avail(sanita, 6, 10, 13);

        var normunds = NewUser("Normunds", "Kārkliņš", UserRole.Dalibnieks, RoleDeveloper, "Serveri");
        Sk(normunds, "Java", 4); Sk(normunds, "SQL / datubāzes", 3); Sk(normunds, "Docker / DevOps", 4);
        Avail(normunds, 1, 18, 22); Avail(normunds, 3, 18, 22);

        // Dalībniece ar nepabeigtu profilu – saskaņošanā tiek parādīta kā neiekļauta.
        var paula = NewUser("Paula", "Jansone", UserRole.Dalibnieks, null, "Mārketings");

        var gustavs = NewUser("Gustavs", "Vilks", UserRole.Dalibnieks, RoleTester, "Kiberdrošība");
        Sk(gustavs, "Testēšana (QA)", 5); Sk(gustavs, "Python", 3);
        Avail(gustavs, 2, 18, 21); Avail(gustavs, 4, 18, 21);

        var users = new List<User>
        {
            organizer, admin, anna, roberts, elina, kristaps, marta, davis, laima, edgars,
            ieva, toms, agnese, rihards, sanita, normunds, paula, gustavs
        };
        db.Users.AddRange(users);

        // ---------- Projekti ----------
        Project NewProject(string title, string description, int size, int deadlineInDays, int endInDays,
            string[] roles, params (string skill, int min)[] required)
        {
            var p = new Project
            {
                Title = title,
                Description = description,
                TeamSize = size,
                ApplicationDeadline = today.AddDays(deadlineInDays),
                EndDate = today.AddDays(endInDays),
                Organizer = organizer,
                RequiredRoles = roles.ToList()
            };
            foreach (var (skill, min) in required)
                p.RequiredSkills.Add(new ProjectSkill { Project = p, Skill = skills[skill], MinLevel = min });
            return p;
        }

        void Apply(Project p, params User[] applicants)
        {
            int i = 0;
            foreach (var u in applicants)
                p.Applications.Add(new Application
                {
                    Project = p,
                    User = u,
                    CreatedAt = DateTime.UtcNow.AddDays(-10).AddHours(i++)
                });
        }

        // 1) Galvenais demonstrācijas projekts – termiņš beidzies, var palaist saskaņošanu.
        var hackathon = NewProject(
            "RTU Hakatons 2026: Viedā pilsēta",
            "48 stundu hakatons, kurā komandas izstrādā tīmekļa risinājumu Rīgas pilsētvides problēmai " +
            "(transports, enerģija, atkritumu šķirošana). Katrai komandai vajadzīgs izstrādātājs, dizainers, " +
            "projekta vadītājs un testētājs.",
            4, -2, 30,
            new[] { RoleDeveloper, RoleDesigner, RoleManager, RoleTester },
            ("C#", 3), ("React", 3), ("UI/UX dizains", 3), ("Testēšana (QA)", 3));
        Apply(hackathon, anna, roberts, elina, kristaps, marta, davis, laima, edgars,
            ieva, toms, agnese, sanita, normunds, rihards, paula);

        // 2) Atvērts projekts – dalībnieki var pieteikties.
        var library = NewProject(
            "Studentu bibliotēkas mobilā lietotne",
            "Mobilajām ierīcēm pielāgota tīmekļa lietotne, kurā students var meklēt grāmatas, rezervēt tās " +
            "un saņemt atgādinājumus par nodošanas termiņu.",
            3, 10, 75,
            new[] { RoleDeveloper, RoleDesigner, RoleTester },
            ("React", 3), ("UI/UX dizains", 3), ("SQL / datubāzes", 2));
        Apply(library, davis, laima, gustavs);

        // 3) Atvērts projekts ar tuvu termiņu.
        var survey = NewProject(
            "Datu analīze: RTU studentu aptauja",
            "Studentu apmierinātības aptaujas datu apstrāde, vizualizācija un rezultātu prezentācija fakultātes vadībai.",
            3, 5, 40,
            new[] { RoleAnalyst, RoleDeveloper, RoleManager },
            ("Python", 3), ("Datu analīze", 3), ("Prezentēšana", 2));
        Apply(survey, edgars, rihards);

        // 4) Pabeigts projekts ar apstiprinātu komandu (dalībnieka skatam "Manas komandas").
        var ngo = NewProject(
            "Tīmekļa vietne biedrībai „Zaļā Rīga”",
            "Brīvprātīgo projekts – vienkārša tīmekļa vietne ar pasākumu kalendāru un pieteikšanās formu.",
            3, -60, -5,
            new[] { RoleDeveloper, RoleDesigner, RoleTester },
            ("C#", 3), ("UI/UX dizains", 3), ("Testēšana (QA)", 3));
        ngo.Status = ProjectStatus.KomandasApstiprinatas;
        Apply(ngo, anna, elina, marta);

        db.Projects.AddRange(hackathon, library, survey, ngo);

        // Saglabājam, lai entītijām tiktu piešķirti ID (vajadzīgi novērtējuma aprēķinam).
        db.SaveChanges();

        var ngoTeam = new Team
        {
            Project = ngo,
            Name = "Komanda 1",
            IsApproved = true,
            CreatedAt = DateTime.UtcNow.AddDays(-58),
            ApprovedAt = DateTime.UtcNow.AddDays(-58)
        };
        foreach (var u in new[] { anna, elina, marta })
            ngoTeam.Members.Add(new TeamMember { Team = ngoTeam, User = u, Role = u.PreferredRole ?? "" });
        MatchingMapper.ApplyScore(ngoTeam, ngo, new MatchingService());
        db.Teams.Add(ngoTeam);

        foreach (var u in new[] { anna, elina, marta })
            db.Notifications.Add(new Notification
            {
                User = u,
                Title = "Komanda apstiprināta",
                Message = $"Tava komanda „{ngoTeam.Name}” projektā „{ngo.Title}” ir apstiprināta. " +
                          $"Tava loma: {u.PreferredRole}.",
                IsRead = true,
                CreatedAt = DateTime.UtcNow.AddDays(-58)
            });

        db.SaveChanges();
    }

    private static string Translit(string s)
    {
        var map = new Dictionary<char, string>
        {
            ['ā'] = "a", ['č'] = "c", ['ē'] = "e", ['ģ'] = "g", ['ī'] = "i", ['ķ'] = "k",
            ['ļ'] = "l", ['ņ'] = "n", ['š'] = "s", ['ū'] = "u", ['ž'] = "z"
        };
        return string.Concat(s.ToLowerInvariant().Select(c => map.TryGetValue(c, out var r) ? r : c.ToString()));
    }
}

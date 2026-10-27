using System.Numerics;

namespace GroupMate.Core.Matching;

/// <summary>
/// Komandu saskaņošanas modulis (prasību specifikācija 6.1.–6.6., projektējums 5.1.5).
///
/// Saderības novērtējums 0–100 sastāv no 4 kritērijiem:
///   1) prasmju atbilstība projekta prasībām – līdz 40 punktiem;
///   2) prasmju dažādība komandā             – līdz 20 punktiem;
///   3) pieejamā laika sakritība             – līdz 20 punktiem;
///   4) lomu sadalījums bez dublēšanās        – līdz 20 punktiem.
///
/// Algoritms:
///   1) dalībnieki ar nepilnīgu profilu netiek iekļauti (tiek parādīti atsevišķi);
///   2) komandu skaits = dalībnieku skaits / komandas lielums;
///   3) spēcīgākie dalībnieki (pēc prasmēm projekta prasībās) tiek izvietoti pa vienam katrā komandā;
///   4) pārējie pēc kārtas tiek ievietoti tajā komandā, kurai tie visvairāk palielina novērtējumu;
///   5) lokālā optimizācija – dalībnieku apmaiņa starp komandām, kamēr kopējais novērtējums uzlabojas.
/// </summary>
public class MatchingService
{
    public const int MaxSkillPoints = 40;
    public const int MaxDiversityPoints = 20;
    public const int MaxAvailabilityPoints = 20;
    public const int MaxRolePoints = 20;

    /// <summary>Kopīgo stundu skaits nedēļā, ar kuru laika kritērijs dod maksimālo punktu skaitu.</summary>
    public const int TargetCommonHours = 6;

    private const int MaxOptimizationPasses = 50;
    private const double Epsilon = 1e-9;

    private readonly Dictionary<int, string> _skillNames;

    /// <param name="skillNames">Prasmju nosaukumi (SkillId → nosaukums) skaidrojumu tekstiem.</param>
    public MatchingService(Dictionary<int, string>? skillNames = null)
    {
        _skillNames = skillNames ?? new Dictionary<int, string>();
    }

    public MatchingResult Run(MatchingProject project, IReadOnlyList<Candidate> applicants)
    {
        if (project.TeamSize < 2 || project.TeamSize > 10)
            throw new MatchingException("Komandas lielumam jābūt no 2 līdz 10 dalībniekiem.");

        var unassigned = new List<UnassignedCandidate>();

        var eligible = new List<Candidate>();
        foreach (var c in applicants)
        {
            if (c.HasCompleteProfile)
                eligible.Add(c);
            else
                unassigned.Add(new UnassignedCandidate
                {
                    Candidate = c,
                    Reason = "Profils nav pilnībā aizpildīts (trūkst prasmju, pieejamā laika vai vēlamās lomas)."
                });
        }

        int teamCount = eligible.Count / project.TeamSize;
        if (teamCount == 0)
            throw new MatchingException(
                $"Nepietiek dalībnieku komandu veidošanai: ar aizpildītu profilu ir {eligible.Count}, " +
                $"bet komandas lielums ir {project.TeamSize}.");

        // 1. Kārtojam dalībniekus pēc prasmju vērtības projektam.
        var ordered = eligible
            .OrderByDescending(c => CandidateValue(c, project))
            .ThenBy(c => c.UserId)
            .ToList();

        // 2. Katrā komandā viens "sākuma" dalībnieks.
        var teams = new List<List<Candidate>>();
        for (int i = 0; i < teamCount; i++)
            teams.Add(new List<Candidate> { ordered[i] });

        // 3. Pārējos ievietojam komandā ar lielāko novērtējuma pieaugumu.
        var leftovers = new List<Candidate>();
        foreach (var c in ordered.Skip(teamCount))
        {
            int bestTeam = -1;
            double bestGain = double.MinValue;
            for (int t = 0; t < teams.Count; t++)
            {
                if (teams[t].Count >= project.TeamSize) continue;
                double before = Score(teams[t], project).Raw;
                double after = Score(teams[t].Append(c).ToList(), project).Raw;
                double gain = after - before;
                // Pie vienāda pieauguma priekšroka mazākai komandai – lai komandas pildītos vienmērīgi.
                if (gain > bestGain + Epsilon ||
                    (Math.Abs(gain - bestGain) <= Epsilon && bestTeam >= 0 && teams[t].Count < teams[bestTeam].Count))
                {
                    bestGain = gain;
                    bestTeam = t;
                }
            }

            if (bestTeam >= 0) teams[bestTeam].Add(c);
            else leftovers.Add(c);
        }

        // 4. Lokālā optimizācija.
        Optimize(teams, leftovers, project);

        foreach (var c in leftovers)
            unassigned.Add(new UnassignedCandidate
            {
                Candidate = c,
                Reason = $"Visas komandas ir pilnas ({teamCount} komandas pa {project.TeamSize} dalībniekiem)."
            });

        return new MatchingResult
        {
            Teams = teams
                .Select(t => new ProposedTeam { Members = t, Score = Score(t, project) })
                .OrderByDescending(t => t.Score.Total)
                .ToList(),
            Unassigned = unassigned,
            SkillGapReport = BuildSkillGapReport(project, eligible, teamCount)
        };
    }

    /// <summary>Aprēķina komandas saderības novērtējumu (izmanto arī pārrēķinam pēc korekcijas).</summary>
    public TeamScore Score(IReadOnlyList<Candidate> members, MatchingProject project)
    {
        var explanation = new List<string>();
        if (members.Count == 0)
            return new TeamScore { Explanation = { "Komandā nav dalībnieku." } };

        // 1) Prasmju atbilstība.
        double skillRatio;
        if (project.RequiredSkills.Count == 0)
        {
            skillRatio = 1;
            explanation.Add("Prasmju atbilstība: projektam nav noteiktas prasmju prasības.");
        }
        else
        {
            double sum = 0;
            var missing = new List<string>();
            foreach (var req in project.RequiredSkills)
            {
                int best = members.Max(m => m.Skills.TryGetValue(req.SkillId, out var lvl) ? lvl : 0);
                sum += Math.Min((double)best / req.MinLevel, 1.0);
                if (best < req.MinLevel)
                    missing.Add(best == 0
                        ? $"{req.SkillName} (nav)"
                        : $"{req.SkillName} ({best}/{req.MinLevel})");
            }
            skillRatio = sum / project.RequiredSkills.Count;
            int covered = project.RequiredSkills.Count - missing.Count;
            explanation.Add(
                $"Prasmju atbilstība: nosegtas {covered} no {project.RequiredSkills.Count} prasībām" +
                (missing.Count > 0 ? $"; nepietiek: {string.Join(", ", missing)}." : "."));
        }

        // 2) Prasmju dažādība – cik dažādas ir dalībnieku galvenās (spēcīgākās) prasmes.
        var requiredIds = project.RequiredSkills.Select(r => r.SkillId).ToHashSet();
        var primary = members.Select(m => PrimarySkill(m, requiredIds)).ToList();
        int distinctPrimary = primary.Distinct().Count();
        double diversityRatio = (double)distinctPrimary / members.Count;
        var duplicatedSkills = primary
            .GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => SkillName(g.Key))
            .ToList();
        explanation.Add(
            $"Prasmju dažādība: {distinctPrimary} dažādas galvenās prasmes {members.Count} dalībniekiem" +
            (duplicatedSkills.Count > 0 ? $"; pārklājas: {string.Join(", ", duplicatedSkills)}." : "."));

        // 3) Pieejamā laika sakritība – stundas nedēļā, kad pieejami visi komandas dalībnieki.
        int commonHours = CommonHours(members);
        double availabilityRatio = Math.Min((double)commonHours / TargetCommonHours, 1.0);
        explanation.Add(
            $"Laika sakritība: kopīgais pieejamais laiks {commonHours} h nedēļā (mērķis ≥ {TargetCommonHours} h).");

        // 4) Lomu sadalījums bez dublēšanās.
        var roles = members
            .Select(m => m.PreferredRole ?? "")
            .Where(r => r != "")
            .ToList();
        var duplicatedRoles = roles
            .GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key} ×{g.Count()}")
            .ToList();
        double roleRatio;
        if (project.RequiredRoles.Count == 0)
        {
            roleRatio = (double)roles.Distinct().Count() / members.Count;
            explanation.Add(
                $"Lomas: {roles.Distinct().Count()} dažādas lomas {members.Count} dalībniekiem" +
                (duplicatedRoles.Count > 0 ? $"; dublējas: {string.Join(", ", duplicatedRoles)}." : "."));
        }
        else
        {
            var coveredRoles = project.RequiredRoles.Where(r => roles.Contains(r)).ToList();
            var missingRoles = project.RequiredRoles.Where(r => !roles.Contains(r)).ToList();
            int needed = Math.Min(project.RequiredRoles.Count, members.Count);
            roleRatio = Math.Min((double)coveredRoles.Count / needed, 1.0);
            explanation.Add(
                $"Lomas: nosegtas {coveredRoles.Count} no {project.RequiredRoles.Count} nepieciešamajām" +
                (missingRoles.Count > 0 ? $"; trūkst: {string.Join(", ", missingRoles)}" : "") +
                (duplicatedRoles.Count > 0 ? $"; dublējas: {string.Join(", ", duplicatedRoles)}" : "") + ".");
        }

        int skillPoints = (int)Math.Round(skillRatio * MaxSkillPoints);
        int diversityPoints = (int)Math.Round(diversityRatio * MaxDiversityPoints);
        int availabilityPoints = (int)Math.Round(availabilityRatio * MaxAvailabilityPoints);
        int rolePoints = (int)Math.Round(roleRatio * MaxRolePoints);

        return new TeamScore
        {
            SkillMatch = skillPoints,
            Diversity = diversityPoints,
            Availability = availabilityPoints,
            Roles = rolePoints,
            Total = skillPoints + diversityPoints + availabilityPoints + rolePoints,
            Raw = skillRatio * MaxSkillPoints + diversityRatio * MaxDiversityPoints +
                  availabilityRatio * MaxAvailabilityPoints + roleRatio * MaxRolePoints,
            Explanation = explanation
        };
    }

    /// <summary>Kopīgās stundas nedēļā, kad pieejami visi dalībnieki.</summary>
    public static int CommonHours(IReadOnlyList<Candidate> members)
    {
        if (members.Count == 0) return 0;
        int total = 0;
        for (int day = 1; day <= 7; day++)
        {
            uint common = 0xFFFFFF; // 24 stundas
            foreach (var m in members)
                common &= DayMask(m, day);
            total += BitOperations.PopCount(common);
        }
        return total;
    }

    private static uint DayMask(Candidate c, int day)
    {
        uint mask = 0;
        foreach (var slot in c.Availability.Where(s => s.DayOfWeek == day))
        {
            int start = Math.Clamp(slot.StartHour, 0, 24);
            int end = Math.Clamp(slot.EndHour, 0, 24);
            for (int h = start; h < end; h++)
                mask |= 1u << h;
        }
        return mask;
    }

    /// <summary>Dalībnieka spēcīgākā prasme; pie vienāda līmeņa priekšroka projektam vajadzīgajai.</summary>
    private static int PrimarySkill(Candidate c, HashSet<int> requiredIds) =>
        c.Skills
            .OrderByDescending(s => s.Value)
            .ThenByDescending(s => requiredIds.Contains(s.Key))
            .ThenBy(s => s.Key)
            .Select(s => s.Key)
            .FirstOrDefault();

    private static double CandidateValue(Candidate c, MatchingProject project) =>
        project.RequiredSkills.Sum(r => c.Skills.TryGetValue(r.SkillId, out var lvl) ? Math.Min(lvl, 5) : 0);

    private static string TeamsWord(int n) => n == 1 ? "1 komandai" : $"{n} komandām";

    private string SkillName(int id) => _skillNames.TryGetValue(id, out var n) ? n : $"prasme #{id}";

    private void Optimize(List<List<Candidate>> teams, List<Candidate> leftovers, MatchingProject project)
    {
        var scores = teams.Select(t => Score(t, project).Raw).ToList();

        for (int pass = 0; pass < MaxOptimizationPasses; pass++)
        {
            bool improved = false;

            // Apmaiņa starp divām komandām.
            for (int a = 0; a < teams.Count; a++)
            for (int b = a + 1; b < teams.Count; b++)
            for (int i = 0; i < teams[a].Count; i++)
            for (int j = 0; j < teams[b].Count; j++)
            {
                var newA = teams[a].ToList();
                var newB = teams[b].ToList();
                (newA[i], newB[j]) = (newB[j], newA[i]);
                double sa = Score(newA, project).Raw;
                double sb = Score(newB, project).Raw;
                if (sa + sb > scores[a] + scores[b] + Epsilon)
                {
                    teams[a] = newA; teams[b] = newB;
                    scores[a] = sa; scores[b] = sb;
                    improved = true;
                }
            }

            // Apmaiņa ar neiekļautu dalībnieku.
            for (int a = 0; a < teams.Count; a++)
            for (int i = 0; i < teams[a].Count; i++)
            for (int j = 0; j < leftovers.Count; j++)
            {
                var newA = teams[a].ToList();
                var outgoing = newA[i];
                newA[i] = leftovers[j];
                double sa = Score(newA, project).Raw;
                if (sa > scores[a] + Epsilon)
                {
                    teams[a] = newA;
                    leftovers[j] = outgoing;
                    scores[a] = sa;
                    improved = true;
                }
            }

            if (!improved) break;
        }
    }

    /// <summary>Prasmju trūkumu atskaite (prasība 6.5.): kuras prasmes/lomas nepietiks visām komandām.</summary>
    public List<string> BuildSkillGapReport(MatchingProject project, IReadOnlyList<Candidate> eligible, int teamCount)
    {
        var report = new List<string>();

        foreach (var req in project.RequiredSkills)
        {
            int matching = eligible.Count(c => c.Skills.TryGetValue(req.SkillId, out var lvl) && lvl >= req.MinLevel);
            if (matching == 0)
                report.Add($"Prasme „{req.SkillName}” (līmenis ≥ {req.MinLevel}): neviens pieteikušais dalībnieks neatbilst prasībai.");
            else if (matching < teamCount)
                report.Add($"Prasme „{req.SkillName}” (līmenis ≥ {req.MinLevel}): atbilst {matching} dalībnieki, " +
                           $"bet komandu ir {teamCount} – {TeamsWord(teamCount - matching)} šīs prasmes trūks.");
        }

        foreach (var role in project.RequiredRoles)
        {
            int matching = eligible.Count(c => c.PreferredRole == role);
            if (matching == 0)
                report.Add($"Loma „{role}”: neviens dalībnieks to nav norādījis kā vēlamo lomu.");
            else if (matching < teamCount)
                report.Add($"Loma „{role}”: to vēlas {matching} dalībnieki, bet komandu ir {teamCount} – " +
                           $"{TeamsWord(teamCount - matching)} šīs lomas trūks.");
        }

        return report;
    }
}

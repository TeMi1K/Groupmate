import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../api";
import { ErrorBox, InfoBox, Loading, ScoreBar, StatusBadge } from "../components";
import { useToast } from "../context";
import { formatDate, formatDateTime } from "../format";
import type { Applicant, MatchingView, Project, Team } from "../types";

/**
 * Organizatora projekta pārvaldība: pieteikumi, komandu saskaņošana, korekcija un apstiprināšana
 * (prasības 6.1.–6.7., 7.2., 8.1.).
 */
export default function OrgProjectPage() {
  const { id } = useParams();
  const toast = useToast();

  const [project, setProject] = useState<Project | null>(null);
  const [applicants, setApplicants] = useState<Applicant[]>([]);
  const [view, setView] = useState<MatchingView | null>(null);
  const [roles, setRoles] = useState<string[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [tab, setTab] = useState<"teams" | "applicants">("teams");

  const loadAll = async () => {
    try {
      const [p, a, v, r] = await Promise.all([
        api<Project>(`/projects/${id}`),
        api<Applicant[]>(`/projects/${id}/applications`),
        api<MatchingView>(`/projects/${id}/teams`),
        api<string[]>("/skills/roles"),
      ]);
      setProject(p);
      setApplicants(a);
      setView(v);
      setRoles(r);
      if (v.teams.length === 0) setTab("applicants");
    } catch (e) {
      setError(errorMessage(e));
    }
  };

  useEffect(() => {
    loadAll();
  }, [id]);

  /** Izpilda darbību, kas atgriež atjaunotu saskaņošanas skatu. */
  const run = async (path: string, method: string, body?: unknown, successText?: string) => {
    setBusy(true);
    setActionError(null);
    try {
      const v = await api<MatchingView>(`/projects/${id}/teams${path}`, { method, body });
      setView(v);
      setTab("teams");
      if (successText) toast(successText);
      const p = await api<Project>(`/projects/${id}`);
      setProject(p);
    } catch (e) {
      setActionError(errorMessage(e));
      toast(errorMessage(e), "error");
    } finally {
      setBusy(false);
    }
  };

  if (error) return <ErrorBox message={error} />;
  if (!project || !view) return <Loading />;

  const hasTeams = view.teams.length > 0;
  const approved = view.isApproved;

  const runMatching = () => {
    if (hasTeams && !confirm("Palaist saskaņošanu no jauna? Pašreizējais priekšlikums un tā izmaiņas tiks aizstāti.")) return;
    run("/matching", "POST", undefined, "Komandu varianti izveidoti.");
  };

  const approve = () => {
    if (!confirm("Apstiprināt komandas? Pēc apstiprināšanas sastāvu mainīt vairs nevarēs un dalībnieki saņems paziņojumus.")) return;
    run("/approve", "POST", undefined, "Komandas apstiprinātas, dalībniekiem nosūtīti paziņojumi.");
  };

  const move = (userId: number, targetTeamId: number | null) =>
    run("/move", "POST", { userId, targetTeamId }, "Sastāvs mainīts, novērtējums pārrēķināts.");

  const changeRole = (teamId: number, userId: number, role: string) =>
    run(`/${teamId}/members/${userId}/role`, "PUT", { role }, "Loma mainīta, novērtējums pārrēķināts.");

  const avg = hasTeams ? Math.round(view.teams.reduce((s, t) => s + t.score, 0) / view.teams.length) : 0;

  return (
    <>
      <p>
        <Link to="/org/projects">← Mani projekti</Link>
      </p>

      <section className="card">
        <div className="page-head">
          <h1>{project.title}</h1>
          <StatusBadge status={project.status} />
        </div>
        <dl className="facts">
          <div>
            <dt>Komandas lielums</dt>
            <dd>{project.teamSize}</dd>
          </div>
          <div>
            <dt>Pieteikšanās termiņš</dt>
            <dd>{formatDate(project.applicationDeadline)}</dd>
          </div>
          <div>
            <dt>Beigas</dt>
            <dd>{formatDate(project.endDate)}</dd>
          </div>
          <div>
            <dt>Pieteikumi</dt>
            <dd>{applicants.length}</dd>
          </div>
        </dl>
        <div className="chips">
          {project.requiredSkills.map((s) => (
            <span key={s.skillId} className="chip">
              {s.name} ≥{s.minLevel}
            </span>
          ))}
          {project.requiredRoles.map((r) => (
            <span key={r} className="chip chip-outline">
              {r}
            </span>
          ))}
        </div>

        {!approved && (
          <div className="actions top-gap">
            <button className="btn btn-primary" disabled={busy || !project.canRunMatching} onClick={runMatching}>
              {hasTeams ? "Veidot komandas no jauna" : "Veidot komandas"}
            </button>
            {hasTeams && (
              <button className="btn" disabled={busy} onClick={approve}>
                Apstiprināt komandas
              </button>
            )}
            {!project.canRunMatching && (
              <span className="muted small">
                Saskaņošanu var palaist pēc pieteikšanās termiņa beigām ({formatDate(project.applicationDeadline)}).
              </span>
            )}
          </div>
        )}
        {approved && <InfoBox>Komandas ir apstiprinātas, dalībnieki saņēmuši paziņojumus.</InfoBox>}
        <ErrorBox message={actionError} />
      </section>

      <div className="tabs">
        <button className={tab === "teams" ? "active" : ""} onClick={() => setTab("teams")}>
          Komandas {hasTeams && `(${view.teams.length})`}
        </button>
        <button className={tab === "applicants" ? "active" : ""} onClick={() => setTab("applicants")}>
          Pieteikumi ({applicants.length})
        </button>
      </div>

      {tab === "applicants" && <ApplicantsTable applicants={applicants} />}

      {tab === "teams" && !hasTeams && (
        <p className="muted">Komandas vēl nav izveidotas. Nospied „Veidot komandas”, lai sistēma piedāvātu sastāvus.</p>
      )}

      {tab === "teams" && hasTeams && (
        <>
          <div className="summary">
            <div>
              <strong>{view.teams.length}</strong> komandas
            </div>
            <div>
              Vidējais novērtējums <strong>{avg}</strong>/100
            </div>
            <div>
              Neiekļauti <strong>{view.unassigned.length}</strong>
            </div>
          </div>

          <div className="team-grid">
            {view.teams.map((t) => (
              <TeamCard
                key={t.id}
                team={t}
                teams={view.teams}
                roles={roles}
                disabled={busy || approved}
                onMove={move}
                onRole={changeRole}
              />
            ))}
          </div>

          <div className="two-col">
            <section className="card">
              <h2>Neiekļautie dalībnieki</h2>
              {view.unassigned.length === 0 && <p className="muted">Visi pieteikušies dalībnieki ir iekļauti komandās.</p>}
              {view.unassigned.map((u) => (
                <div key={u.userId} className="unassigned">
                  <div>
                    <strong>{u.fullName}</strong> {u.preferredRole && <span className="muted">· {u.preferredRole}</span>}
                    <div className="muted small">{u.reason}</div>
                  </div>
                  {!approved && (
                    <select
                      aria-label="Pievienot komandai"
                      value=""
                      disabled={busy}
                      onChange={(e) => e.target.value && move(u.userId, Number(e.target.value))}
                    >
                      <option value="">Pievienot komandai…</option>
                      {view.teams.map((t) => (
                        <option key={t.id} value={t.id}>
                          {t.name}
                        </option>
                      ))}
                    </select>
                  )}
                </div>
              ))}
            </section>

            <section className="card">
              <h2>Prasmju trūkumu atskaite</h2>
              {view.skillGapReport.length === 0 ? (
                <p className="muted">Visas projekta prasības un lomas var nosegt visām komandām.</p>
              ) : (
                <ul className="gap-list">
                  {view.skillGapReport.map((g) => (
                    <li key={g}>{g}</li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        </>
      )}
    </>
  );
}

function TeamCard({
  team,
  teams,
  roles,
  disabled,
  onMove,
  onRole,
}: {
  team: Team;
  teams: Team[];
  roles: string[];
  disabled: boolean;
  onMove: (userId: number, targetTeamId: number | null) => void;
  onRole: (teamId: number, userId: number, role: string) => void;
}) {
  return (
    <section className="card team">
      <div className="team-head">
        <h3>{team.name}</h3>
        <div className="score-big">
          {team.score}
          <small>/100</small>
        </div>
      </div>
      <ScoreBar value={team.score} />

      <div className="criteria">
        <ScoreBar label="Prasmju atbilstība" value={team.skillScore} max={40} />
        <ScoreBar label="Prasmju dažādība" value={team.diversityScore} max={20} />
        <ScoreBar label="Laika sakritība" value={team.availabilityScore} max={20} />
        <ScoreBar label="Lomu sadalījums" value={team.roleScore} max={20} />
      </div>

      <details className="explanation">
        <summary>Kāpēc šāds sastāvs?</summary>
        <ul>
          {team.explanation.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      </details>

      <ul className="members">
        {team.members.map((m) => (
          <li key={m.userId}>
            <div className="member-main">
              <strong>{m.fullName}</strong>
              <div className="muted small">{m.skills.map((s) => `${s.name} ${s.level}`).join(" · ")}</div>
            </div>
            <div className="member-controls">
              <select
                aria-label={`${m.fullName} loma`}
                value={m.role}
                disabled={disabled}
                onChange={(e) => onRole(team.id, m.userId, e.target.value)}
              >
                {roles.map((r) => (
                  <option key={r}>{r}</option>
                ))}
              </select>
              <select
                aria-label={`Pārvietot ${m.fullName}`}
                value=""
                disabled={disabled}
                onChange={(e) => {
                  const v = e.target.value;
                  if (v === "") return;
                  onMove(m.userId, v === "out" ? null : Number(v));
                }}
              >
                <option value="">Pārvietot…</option>
                {teams
                  .filter((t) => t.id !== team.id)
                  .map((t) => (
                    <option key={t.id} value={t.id}>
                      → {t.name}
                    </option>
                  ))}
                <option value="out">Izņemt no komandas</option>
              </select>
            </div>
          </li>
        ))}
      </ul>
      {team.isApproved && <p className="muted small">Apstiprināta</p>}
    </section>
  );
}

function ApplicantsTable({ applicants }: { applicants: Applicant[] }) {
  if (applicants.length === 0) return <p className="muted">Vēl neviens nav pieteicies.</p>;
  return (
    <div className="table-wrap">
      <table className="table">
        <thead>
          <tr>
            <th>Dalībnieks</th>
            <th>Vēlamā loma</th>
            <th>Prasmes</th>
            <th>Profils</th>
            <th>Pieteicās</th>
          </tr>
        </thead>
        <tbody>
          {applicants.map((a) => (
            <tr key={a.userId}>
              <td className="strong">{a.fullName}</td>
              <td>{a.preferredRole ?? <span className="muted">—</span>}</td>
              <td>
                <div className="chips">
                  {a.skills.map((s) => (
                    <span key={s.skillId} className="chip">
                      {s.name} {s.level}
                    </span>
                  ))}
                </div>
              </td>
              <td>{a.profileComplete ? "Aizpildīts" : <span className="text-error">Nepilnīgs</span>}</td>
              <td className="nowrap">{formatDateTime(a.appliedAt)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

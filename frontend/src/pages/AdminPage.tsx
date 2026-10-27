import { useEffect, useState } from "react";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading, PrototypeTag } from "../components";
import { useNotImplemented } from "../context";
import { ROLE_LABELS, formatDate } from "../format";
import type { AdminUser, Skill, Stats } from "../types";

/**
 * Administrēšana (prasības 10.1.–10.5.).
 * Dati tiek nolasīti no datubāzes, bet izmaiņu darbības prototipā ir tikai vizuālas.
 */
export default function AdminPage() {
  const notImplemented = useNotImplemented();
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [stats, setStats] = useState<Stats | null>(null);
  const [skills, setSkills] = useState<Skill[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    Promise.all([api<AdminUser[]>("/admin/users"), api<Stats>("/admin/stats"), api<Skill[]>("/skills")])
      .then(([u, s, sk]) => {
        setUsers(u);
        setStats(s);
        setSkills(sk);
      })
      .catch((e) => setError(errorMessage(e)));
  }, []);

  if (error) return <ErrorBox message={error} />;
  if (!users || !stats) return <Loading />;

  return (
    <>
      <div className="page-head">
        <h1>Administrēšana</h1>
      </div>

      <div className="stats">
        <Stat label="Lietotāji" value={stats.users} />
        <Stat label="Dalībnieki" value={stats.participants} />
        <Stat label="Organizatori" value={stats.organizers} />
        <Stat label="Projekti" value={stats.projects} />
        <Stat label="Atvērtie projekti" value={stats.openProjects} />
        <Stat label="Apstiprinātās komandas" value={stats.approvedTeams} />
      </div>

      <section className="card">
        <h2>
          Lietotāji <PrototypeTag>Lomu maiņa un bloķēšana – tikai vizuāli</PrototypeTag>
        </h2>
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Vārds</th>
                <th>E-pasts</th>
                <th>Loma</th>
                <th>Statuss</th>
                <th>Reģistrēts</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map((u) => (
                <tr key={u.id}>
                  <td className="strong">{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>
                    <select value={u.role} onChange={notImplemented} aria-label="Loma">
                      {Object.entries(ROLE_LABELS).map(([v, l]) => (
                        <option key={v} value={v}>
                          {l}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>{u.isBlocked ? <span className="text-error">Bloķēts</span> : "Aktīvs"}</td>
                  <td className="nowrap">{formatDate(u.createdAt)}</td>
                  <td>
                    <button className="btn btn-small btn-danger" onClick={notImplemented}>
                      Bloķēt
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="card">
        <h2>
          Prasmju katalogs <PrototypeTag>Papildināšana – tikai vizuāli</PrototypeTag>
        </h2>
        <form
          className="row row-tight"
          onSubmit={(e) => {
            e.preventDefault();
            notImplemented();
          }}
        >
          <input placeholder="Jaunas prasmes nosaukums" />
          <select aria-label="Kategorija">
            {[...new Set(skills.map((s) => s.category))].map((c) => (
              <option key={c}>{c}</option>
            ))}
          </select>
          <button className="btn btn-small" type="submit">
            Pievienot
          </button>
        </form>
        <div className="chips top-gap">
          {skills.map((s) => (
            <span key={s.id} className="chip" title={s.category}>
              {s.name}
            </span>
          ))}
        </div>
      </section>
    </>
  );
}

function Stat({ label, value }: { label: string; value: number }) {
  return (
    <div className="stat card">
      <div className="stat-value">{value}</div>
      <div className="muted small">{label}</div>
    </div>
  );
}

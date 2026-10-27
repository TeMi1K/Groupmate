import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading, StatusBadge } from "../components";
import { useToast } from "../context";
import { formatDate } from "../format";
import type { Project, Skill } from "../types";

/** Atvērto projektu pārskats ar meklēšanu un filtrēšanu (prasības 5.1.–5.3.). */
export default function ProjectsPage() {
  const toast = useToast();
  const [projects, setProjects] = useState<Project[] | null>(null);
  const [skills, setSkills] = useState<Skill[]>([]);
  const [search, setSearch] = useState("");
  const [skillId, setSkillId] = useState("");
  const [onlyOpen, setOnlyOpen] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<number | null>(null);

  const load = () => {
    const params = new URLSearchParams();
    if (search.trim()) params.set("search", search.trim());
    if (skillId) params.set("skillId", skillId);
    if (onlyOpen) params.set("onlyOpen", "true");
    api<Project[]>(`/projects?${params}`)
      .then((p) => {
        setProjects(p);
        setError(null);
      })
      .catch((e) => setError(errorMessage(e)));
  };

  useEffect(() => {
    api<Skill[]>("/skills").then(setSkills).catch(() => {});
  }, []);

  // Meklēšana ar nelielu aizturi, lai nesūtītu pieprasījumu katram burtam.
  useEffect(() => {
    const t = setTimeout(load, 250);
    return () => clearTimeout(t);
  }, [search, skillId, onlyOpen]);

  const apply = async (p: Project) => {
    setBusyId(p.id);
    try {
      const res = await api<{ message: string }>(`/projects/${p.id}/applications`, { method: "POST" });
      toast(res.message);
      load();
    } catch (e) {
      toast(errorMessage(e), "error");
    } finally {
      setBusyId(null);
    }
  };

  const withdraw = async (p: Project) => {
    setBusyId(p.id);
    try {
      const res = await api<{ message: string }>(`/projects/${p.id}/applications`, { method: "DELETE" });
      toast(res.message);
      load();
    } catch (e) {
      toast(errorMessage(e), "error");
    } finally {
      setBusyId(null);
    }
  };

  return (
    <>
      <div className="page-head">
        <h1>Projekti</h1>
      </div>

      <div className="filters card">
        <label>
          Meklēt pēc nosaukuma
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="piem., hakatons" />
        </label>
        <label>
          Nepieciešamā prasme
          <select value={skillId} onChange={(e) => setSkillId(e.target.value)}>
            <option value="">Visas prasmes</option>
            {skills.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
        </label>
        <label className="checkbox">
          <input type="checkbox" checked={onlyOpen} onChange={(e) => setOnlyOpen(e.target.checked)} />
          <span>Tikai projekti, kuriem var pieteikties</span>
        </label>
      </div>

      <ErrorBox message={error} />
      {projects === null && !error && <Loading />}
      {projects && projects.length === 0 && <p className="muted">Nav atrasts neviens projekts.</p>}

      {projects && projects.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Projekts</th>
                <th>Nepieciešamās prasmes</th>
                <th>Komanda</th>
                <th>Pieteikšanās līdz</th>
                <th>Statuss</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {projects.map((p) => (
                <tr key={p.id}>
                  <td>
                    <Link to={`/projects/${p.id}`} className="strong">
                      {p.title}
                    </Link>
                    <div className="muted small">{p.organizerName}</div>
                  </td>
                  <td>
                    <div className="chips">
                      {p.requiredSkills.map((s) => (
                        <span key={s.skillId} className="chip">
                          {s.name} ≥{s.minLevel}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td className="nowrap">{p.teamSize} cilv.</td>
                  <td className="nowrap">{formatDate(p.applicationDeadline)}</td>
                  <td>
                    <StatusBadge status={p.status} />
                  </td>
                  <td className="nowrap">
                    {p.myApplicationStatus === "Aktivs" ? (
                      <div className="actions">
                        <span className="badge badge-primary">✓ Pieteicies</span>
                        {p.isOpenForApplications && (
                          <button className="btn btn-small" disabled={busyId === p.id} onClick={() => withdraw(p)}>
                            Atsaukt
                          </button>
                        )}
                      </div>
                    ) : p.isOpenForApplications ? (
                      <button className="btn btn-small btn-primary" disabled={busyId === p.id} onClick={() => apply(p)}>
                        Pieteikties
                      </button>
                    ) : (
                      <span className="muted small">Pieteikšanās slēgta</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

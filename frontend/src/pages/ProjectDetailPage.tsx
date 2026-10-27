import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading, StatusBadge } from "../components";
import { useToast } from "../context";
import { formatDate } from "../format";
import type { Project } from "../types";

/** Projekta skats ar pieteikšanās pogu. */
export default function ProjectDetailPage() {
  const { id } = useParams();
  const toast = useToast();
  const [project, setProject] = useState<Project | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const load = () =>
    api<Project>(`/projects/${id}`)
      .then(setProject)
      .catch((e) => setError(errorMessage(e)));

  useEffect(() => {
    load();
  }, [id]);

  const act = async (method: "POST" | "DELETE") => {
    setBusy(true);
    try {
      const res = await api<{ message: string }>(`/projects/${id}/applications`, { method });
      toast(res.message);
      await load();
    } catch (e) {
      toast(errorMessage(e), "error");
    } finally {
      setBusy(false);
    }
  };

  if (error) return <ErrorBox message={error} />;
  if (!project) return <Loading />;

  const applied = project.myApplicationStatus === "Aktivs";

  return (
    <>
      <p>
        <Link to="/projects">← Atpakaļ uz projektiem</Link>
      </p>
      <div className="card">
        <div className="page-head">
          <h1>{project.title}</h1>
          <StatusBadge status={project.status} />
        </div>
        <p>{project.description}</p>

        <dl className="facts">
          <div>
            <dt>Organizators</dt>
            <dd>{project.organizerName}</dd>
          </div>
          <div>
            <dt>Komandas lielums</dt>
            <dd>{project.teamSize} dalībnieki</dd>
          </div>
          <div>
            <dt>Pieteikšanās termiņš</dt>
            <dd>{formatDate(project.applicationDeadline)}</dd>
          </div>
          <div>
            <dt>Projekta beigas</dt>
            <dd>{formatDate(project.endDate)}</dd>
          </div>
          <div>
            <dt>Pieteikušies</dt>
            <dd>{project.applicantCount}</dd>
          </div>
        </dl>

        <h3>Nepieciešamās prasmes</h3>
        <div className="chips">
          {project.requiredSkills.map((s) => (
            <span key={s.skillId} className="chip">
              {s.name} – līmenis ≥ {s.minLevel}
            </span>
          ))}
        </div>

        <h3>Nepieciešamās lomas</h3>
        <div className="chips">
          {project.requiredRoles.map((r) => (
            <span key={r} className="chip">
              {r}
            </span>
          ))}
        </div>

        <div className="actions top-gap">
          {applied ? (
            <>
              <span className="badge badge-primary">✓ Tu esi pieteicies</span>
              {project.isOpenForApplications && (
                <button className="btn" disabled={busy} onClick={() => act("DELETE")}>
                  Atsaukt pieteikumu
                </button>
              )}
            </>
          ) : (
            <button
              className="btn btn-primary"
              disabled={busy || !project.isOpenForApplications}
              onClick={() => act("POST")}
            >
              Pieteikties projektam
            </button>
          )}
          {!project.isOpenForApplications && !applied && (
            <span className="muted small">Pieteikšanās termiņš ir beidzies.</span>
          )}
        </div>
      </div>
    </>
  );
}

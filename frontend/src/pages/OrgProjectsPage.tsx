import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading, StatusBadge } from "../components";
import { formatDate } from "../format";
import type { Project } from "../types";

/** Organizatora projektu saraksts. */
export default function OrgProjectsPage() {
  const [projects, setProjects] = useState<Project[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<Project[]>("/projects?scope=mine")
      .then(setProjects)
      .catch((e) => setError(errorMessage(e)));
  }, []);

  return (
    <>
      <div className="page-head">
        <h1>Mani projekti</h1>
        <Link to="/org/projects/new" className="btn btn-primary">
          + Jauns projekts
        </Link>
      </div>
      <ErrorBox message={error} />
      {projects === null && !error && <Loading />}
      {projects && projects.length === 0 && <p className="muted">Tev vēl nav neviena projekta.</p>}
      {projects && projects.length > 0 && (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Projekts</th>
                <th>Komanda</th>
                <th>Pieteikšanās līdz</th>
                <th>Beigas</th>
                <th>Pieteikumi</th>
                <th>Statuss</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {projects.map((p) => (
                <tr key={p.id}>
                  <td className="strong">{p.title}</td>
                  <td className="nowrap">{p.teamSize} cilv.</td>
                  <td className="nowrap">{formatDate(p.applicationDeadline)}</td>
                  <td className="nowrap">{formatDate(p.endDate)}</td>
                  <td>{p.applicantCount}</td>
                  <td>
                    <StatusBadge status={p.status} />
                  </td>
                  <td className="nowrap">
                    <Link to={`/org/projects/${p.id}`} className="btn btn-small">
                      {p.canRunMatching ? "Veidot komandas" : "Atvērt"}
                    </Link>
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

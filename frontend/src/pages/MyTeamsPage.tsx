import { useEffect, useState } from "react";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading, PrototypeTag } from "../components";
import { useNotImplemented } from "../context";
import { formatDate, formatDateTime } from "../format";
import type { MyTeam } from "../types";

/** Dalībnieka komandas: sastāvs, lomas un termiņi (prasība 7.1.); atsauksmes (7.3.) – vizuāli. */
export default function MyTeamsPage() {
  const [teams, setTeams] = useState<MyTeam[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api<MyTeam[]>("/my/teams")
      .then(setTeams)
      .catch((e) => setError(errorMessage(e)));
  }, []);

  return (
    <>
      <div className="page-head">
        <h1>Manas komandas</h1>
      </div>
      <ErrorBox message={error} />
      {teams === null && !error && <Loading />}
      {teams && teams.length === 0 && (
        <p className="muted">
          Tu vēl neesi nevienā apstiprinātā komandā. Kad organizators apstiprinās komandas, tās parādīsies šeit.
        </p>
      )}
      {teams?.map((t) => (
        <section key={t.teamId} className="card">
          <div className="page-head">
            <div>
              <h2>{t.projectTitle}</h2>
              <p className="muted small">
                {t.teamName} · Tava loma: <strong>{t.myRole}</strong> · Projekta beigas: {formatDate(t.endDate)}
                {t.approvedAt && <> · Apstiprināta {formatDateTime(t.approvedAt)}</>}
              </p>
            </div>
            <div className="score-big" title="Saderības novērtējums">
              {t.score}
              <small>/100</small>
            </div>
          </div>

          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Dalībnieks</th>
                  <th>Loma</th>
                  <th>E-pasts</th>
                  <th>Galvenās prasmes</th>
                </tr>
              </thead>
              <tbody>
                {t.members.map((m) => (
                  <tr key={m.userId}>
                    <td className="strong">{m.fullName}</td>
                    <td>{m.role}</td>
                    <td>{m.email ? <a href={`mailto:${m.email}`}>{m.email}</a> : <span className="muted">slēpts</span>}</td>
                    <td>
                      <div className="chips">
                        {m.skills.map((s) => (
                          <span key={s.name} className="chip">
                            {s.name} {s.level}
                          </span>
                        ))}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {t.projectEnded && <FeedbackForm />}
        </section>
      ))}
    </>
  );
}

function FeedbackForm() {
  const notImplemented = useNotImplemented();
  const [rating, setRating] = useState(0);

  return (
    <form
      className="feedback"
      onSubmit={(e) => {
        e.preventDefault();
        notImplemented();
      }}
    >
      <h3>
        Novērtē komandas darbu <PrototypeTag />
      </h3>
      <div className="stars" role="radiogroup" aria-label="Vērtējums">
        {[1, 2, 3, 4, 5].map((n) => (
          <button
            type="button"
            key={n}
            className={n <= rating ? "star on" : "star"}
            onClick={() => setRating(n)}
            aria-label={`${n} no 5`}
          >
            ★
          </button>
        ))}
      </div>
      <textarea placeholder="Komentārs (nav obligāts)" rows={2} />
      <button className="btn btn-primary btn-small" type="submit">
        Iesniegt atsauksmi
      </button>
    </form>
  );
}

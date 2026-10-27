import { useEffect, useState } from "react";
import { Link, Navigate, useNavigate } from "react-router-dom";
import { api, errorMessage } from "../api";
import { homeFor } from "../App";
import { ErrorBox, PrototypeTag } from "../components";
import { useNotImplemented, useSession, useToast } from "../context";
import { ROLE_LABELS } from "../format";
import type { DemoUser, Role } from "../types";

const ROLE_ORDER: Role[] = ["Dalibnieks", "Organizators", "Administrators"];

export default function LoginPage() {
  const { session, login } = useSession();
  const navigate = useNavigate();
  const toast = useToast();
  const notImplemented = useNotImplemented();
  const [users, setUsers] = useState<DemoUser[]>([]);
  const [error, setError] = useState<string | null>(null);

  const loadUsers = () =>
    api<DemoUser[]>("/demo/users")
      .then((u) => {
        setUsers(u);
        setError(null);
      })
      .catch((e) => setError(errorMessage(e)));

  useEffect(() => {
    loadUsers();
  }, []);

  if (session) return <Navigate to={homeFor(session.role)} replace />;

  const choose = (u: DemoUser) => {
    login({ id: u.id, fullName: u.fullName, role: u.role });
    navigate(homeFor(u.role));
  };

  const reset = async () => {
    if (!confirm("Atjaunot datubāzi sākotnējā stāvoklī? Visas veiktās izmaiņas tiks dzēstas.")) return;
    try {
      await api("/demo/reset", { method: "POST" });
      toast("Datubāze atjaunota sākotnējā stāvoklī.");
      loadUsers();
    } catch (e) {
      toast(errorMessage(e), "error");
    }
  };

  return (
    <div className="login-page">
      <div className="login-brand">
        <h1>
          Group<span>Mate</span>
        </h1>
        <p>Platforma līdzsvarotu komandu veidošanai studiju projektiem un hakatoniem.</p>
      </div>

      <div className="login-grid">
        <section className="card">
          <h2>
            Pieslēgšanās <PrototypeTag />
          </h2>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              notImplemented();
            }}
          >
            <label>
              E-pasts
              <input type="email" placeholder="vards.uzvards@example.lv" />
            </label>
            <label>
              Parole
              <input type="password" placeholder="••••••••" />
            </label>
            <button className="btn btn-primary btn-block" type="submit">
              Pieslēgties
            </button>
          </form>
          <button className="btn btn-block" onClick={notImplemented}>
            Pieslēgties ar organizācijas kontu (SSO)
          </button>
          <p className="small">
            <a href="#" onClick={(e) => { e.preventDefault(); notImplemented(); }}>Aizmirsi paroli?</a>
            {" · "}
            <Link to="/register">Reģistrēties</Link>
          </p>
        </section>

        <section className="card">
          <h2>Demonstrācijas lietotāji</h2>
          <p className="muted small">
            Prototipā īstas autentifikācijas vietā izvēlies lietotāju, kura vārdā strādāt. Piekļuves tiesības pēc lomas
            pārbauda serveris.
          </p>
          <ErrorBox message={error} />
          {ROLE_ORDER.map((role) => {
            const list = users.filter((u) => u.role === role);
            if (list.length === 0) return null;
            return (
              <div key={role} className="demo-group">
                <h3>{ROLE_LABELS[role]}</h3>
                <div className="demo-users">
                  {list.map((u) => (
                    <button key={u.id} className="demo-user" onClick={() => choose(u)}>
                      <strong>{u.fullName}</strong>
                      <small>{u.preferredRole ?? u.email}</small>
                    </button>
                  ))}
                </div>
              </div>
            );
          })}
          <button className="btn btn-small btn-ghost top-gap" onClick={reset}>
            Atjaunot demo datus
          </button>
        </section>
      </div>
    </div>
  );
}

import { useEffect, useState } from "react";
import { NavLink, Outlet, useLocation, useNavigate } from "react-router-dom";
import { api } from "./api";
import { useSession } from "./context";
import { ROLE_LABELS } from "./format";
import type { Notification } from "./types";

export default function Layout() {
  const { session, logout } = useSession();
  const navigate = useNavigate();
  const location = useLocation();
  const [unread, setUnread] = useState(0);
  const [menuOpen, setMenuOpen] = useState(false);

  // Nelasīto paziņojumu skaits tiek atjaunots katrā lapas maiņā.
  useEffect(() => {
    setMenuOpen(false);
    if (!session) return;
    api<Notification[]>("/my/notifications")
      .then((list) => setUnread(list.filter((n) => !n.isRead).length))
      .catch(() => setUnread(0));
  }, [location.pathname, session]);

  if (!session) return null;

  const links =
    session.role === "Dalibnieks"
      ? [
          { to: "/projects", label: "Projekti" },
          { to: "/my-teams", label: "Manas komandas" },
          { to: "/profile", label: "Mans profils" },
        ]
      : session.role === "Organizators"
        ? [
            { to: "/org/projects", label: "Mani projekti" },
            { to: "/org/projects/new", label: "Jauns projekts" },
          ]
        : [{ to: "/admin", label: "Administrēšana" }];

  return (
    <div className="app">
      <header className="header">
        <div className="header-inner">
          <NavLink to="/" className="logo">
            Group<span>Mate</span>
          </NavLink>
          <button className="menu-toggle" onClick={() => setMenuOpen((o) => !o)} aria-label="Izvēlne">
            ☰
          </button>
          <nav className={`nav ${menuOpen ? "nav-open" : ""}`}>
            {links.map((l) => (
              <NavLink key={l.to} to={l.to} end className={({ isActive }) => (isActive ? "active" : "")}>
                {l.label}
              </NavLink>
            ))}
            <NavLink to="/notifications" className={({ isActive }) => (isActive ? "active" : "")}>
              Paziņojumi{unread > 0 && <span className="count">{unread}</span>}
            </NavLink>
            <div className="user">
              <span>
                {session.fullName} <small>({ROLE_LABELS[session.role]})</small>
              </span>
              <button
                className="btn btn-small btn-ghost"
                onClick={() => {
                  logout();
                  navigate("/login");
                }}
              >
                Iziet
              </button>
            </div>
          </nav>
        </div>
      </header>
      <main className="main">
        <Outlet />
      </main>
      <footer className="footer">GroupMate prototips · RTU „Programmatūras izstrādes tehnoloģija” · 2026</footer>
    </div>
  );
}

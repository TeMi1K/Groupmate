import { Navigate, Route, Routes } from "react-router-dom";
import Layout from "./Layout";
import { useSession } from "./context";
import type { Role } from "./types";
import LoginPage from "./pages/LoginPage";
import RegisterPage from "./pages/RegisterPage";
import ProjectsPage from "./pages/ProjectsPage";
import ProjectDetailPage from "./pages/ProjectDetailPage";
import ProfilePage from "./pages/ProfilePage";
import MyTeamsPage from "./pages/MyTeamsPage";
import NotificationsPage from "./pages/NotificationsPage";
import OrgProjectsPage from "./pages/OrgProjectsPage";
import NewProjectPage from "./pages/NewProjectPage";
import OrgProjectPage from "./pages/OrgProjectPage";
import AdminPage from "./pages/AdminPage";

export function homeFor(role: Role): string {
  switch (role) {
    case "Organizators":
      return "/org/projects";
    case "Administrators":
      return "/admin";
    default:
      return "/projects";
  }
}

function RequireRole({ roles, children }: { roles: Role[]; children: JSX.Element }) {
  const { session } = useSession();
  if (!session) return <Navigate to="/login" replace />;
  if (!roles.includes(session.role)) return <Navigate to={homeFor(session.role)} replace />;
  return children;
}

export default function App() {
  const { session } = useSession();

  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      <Route element={session ? <Layout /> : <Navigate to="/login" replace />}>
        {/* Dalībnieks */}
        <Route path="/projects" element={<RequireRole roles={["Dalibnieks"]}><ProjectsPage /></RequireRole>} />
        <Route path="/projects/:id" element={<RequireRole roles={["Dalibnieks"]}><ProjectDetailPage /></RequireRole>} />
        <Route path="/profile" element={<RequireRole roles={["Dalibnieks"]}><ProfilePage /></RequireRole>} />
        <Route path="/my-teams" element={<RequireRole roles={["Dalibnieks"]}><MyTeamsPage /></RequireRole>} />
        <Route path="/notifications" element={<NotificationsPage />} />

        {/* Organizators */}
        <Route path="/org/projects" element={<RequireRole roles={["Organizators"]}><OrgProjectsPage /></RequireRole>} />
        <Route path="/org/projects/new" element={<RequireRole roles={["Organizators"]}><NewProjectPage /></RequireRole>} />
        <Route path="/org/projects/:id" element={<RequireRole roles={["Organizators"]}><OrgProjectPage /></RequireRole>} />

        {/* Administrators */}
        <Route path="/admin" element={<RequireRole roles={["Administrators"]}><AdminPage /></RequireRole>} />
      </Route>

      <Route path="*" element={<Navigate to={session ? homeFor(session.role) : "/login"} replace />} />
    </Routes>
  );
}

import { useEffect, useState } from "react";
import { api, errorMessage } from "../api";
import { ErrorBox, Loading } from "../components";
import { useToast } from "../context";
import { formatDateTime } from "../format";
import type { Notification } from "../types";

/** Sistēmas paziņojumi (prasība 8.1.). */
export default function NotificationsPage() {
  const toast = useToast();
  const [list, setList] = useState<Notification[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = () =>
    api<Notification[]>("/my/notifications")
      .then(setList)
      .catch((e) => setError(errorMessage(e)));

  useEffect(() => {
    load();
  }, []);

  const markAll = async () => {
    try {
      await api("/my/notifications/read-all", { method: "POST" });
      toast("Visi paziņojumi atzīmēti kā izlasīti.");
      load();
    } catch (e) {
      toast(errorMessage(e), "error");
    }
  };

  return (
    <>
      <div className="page-head">
        <h1>Paziņojumi</h1>
        {list && list.some((n) => !n.isRead) && (
          <button className="btn btn-small" onClick={markAll}>
            Atzīmēt visus kā izlasītus
          </button>
        )}
      </div>
      <ErrorBox message={error} />
      {list === null && !error && <Loading />}
      {list && list.length === 0 && <p className="muted">Paziņojumu nav.</p>}
      {list?.map((n) => (
        <article key={n.id} className={`card notification ${n.isRead ? "" : "unread"}`}>
          <div className="page-head">
            <h3>{n.title}</h3>
            <span className="muted small">{formatDateTime(n.createdAt)}</span>
          </div>
          <p>{n.message}</p>
          <p className="muted small">E-pasts: {n.emailStatus}</p>
        </article>
      ))}
    </>
  );
}

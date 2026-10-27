import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import { loadSession, saveSession } from "./api";
import type { Session } from "./types";

// ---------- Sesija (demonstrācijas lietotājs) ----------

interface SessionContextValue {
  session: Session | null;
  login: (s: Session) => void;
  logout: () => void;
}

const SessionContext = createContext<SessionContextValue | null>(null);

export function SessionProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => loadSession());

  const login = useCallback((s: Session) => {
    saveSession(s);
    setSession(s);
  }, []);

  const logout = useCallback(() => {
    saveSession(null);
    setSession(null);
  }, []);

  return <SessionContext.Provider value={{ session, login, logout }}>{children}</SessionContext.Provider>;
}

export function useSession() {
  const ctx = useContext(SessionContext);
  if (!ctx) throw new Error("useSession jāizmanto SessionProvider iekšienē");
  return ctx;
}

// ---------- Īsi paziņojumi ekrāna stūrī ----------

interface Toast {
  id: number;
  text: string;
  kind: "info" | "error";
}

const ToastContext = createContext<(text: string, kind?: "info" | "error") => void>(() => {});

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);

  const show = useCallback((text: string, kind: "info" | "error" = "info") => {
    const id = Date.now() + Math.random();
    setToasts((t) => [...t, { id, text, kind }]);
    setTimeout(() => setToasts((t) => t.filter((x) => x.id !== id)), 4000);
  }, []);

  return (
    <ToastContext.Provider value={show}>
      {children}
      <div className="toasts" role="status" aria-live="polite">
        {toasts.map((t) => (
          <div key={t.id} className={`toast ${t.kind === "error" ? "toast-error" : ""}`}>
            {t.text}
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export function useToast() {
  return useContext(ToastContext);
}

/** Paziņojums vizuālajām (prototipā nerealizētajām) darbībām. */
export function useNotImplemented() {
  const toast = useToast();
  return () => toast("Šī darbība prototipā ir parādīta tikai vizuāli un netiek saglabāta.");
}

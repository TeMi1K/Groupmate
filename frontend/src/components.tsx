import type { ReactNode } from "react";

export function PrototypeTag({ children = "Tikai vizuāli (prototips)" }: { children?: ReactNode }) {
  return (
    <span className="tag-visual" title="Šī daļa prototipā ir parādīta ar iekodētiem datiem vai bez saglabāšanas">
      {children}
    </span>
  );
}

export function ErrorBox({ message }: { message: string | null }) {
  if (!message) return null;
  return (
    <div className="alert alert-error" role="alert">
      {message}
    </div>
  );
}

export function InfoBox({ children }: { children: ReactNode }) {
  return <div className="alert alert-info">{children}</div>;
}

export function Loading() {
  return <p className="muted">Ielādē…</p>;
}

/** Saderības novērtējuma josla. */
export function ScoreBar({ value, max = 100, label }: { value: number; max?: number; label?: string }) {
  const pct = Math.round((value / max) * 100);
  return (
    <div className="scorebar" title={`${value} no ${max}`}>
      {label && (
        <div className="scorebar-label">
          <span>{label}</span>
          <span>
            {value}/{max}
          </span>
        </div>
      )}
      <div className="scorebar-track">
        <div className="scorebar-fill" style={{ width: `${pct}%` }} />
      </div>
    </div>
  );
}

export function StatusBadge({ status }: { status: string }) {
  const cls =
    status === "Pieteikšanās atvērta"
      ? "badge badge-primary"
      : status === "Gaida komandu veidošanu"
        ? "badge badge-warn"
        : "badge";
  return <span className={cls}>{status}</span>;
}

import type { Role } from "./types";

export const DAYS = ["Pirmdiena", "Otrdiena", "Trešdiena", "Ceturtdiena", "Piektdiena", "Sestdiena", "Svētdiena"];
export const DAYS_SHORT = ["P", "O", "T", "C", "Pk", "S", "Sv"];

export const ROLE_LABELS: Record<Role, string> = {
  Dalibnieks: "Dalībnieks",
  Organizators: "Organizators",
  Administrators: "Administrators",
};

/** "2026-11-05" → "05.11.2026" */
export function formatDate(iso: string): string {
  const [y, m, d] = iso.slice(0, 10).split("-");
  return `${d}.${m}.${y}`;
}

export function formatDateTime(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString("lv-LV", { dateStyle: "short", timeStyle: "short" });
}

export function hour(h: number): string {
  return `${String(h).padStart(2, "0")}:00`;
}

export function levelDots(level: number): string {
  return "●".repeat(level) + "○".repeat(5 - level);
}

export function todayIso(offsetDays = 0): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  return d.toISOString().slice(0, 10);
}

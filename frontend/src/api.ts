import type { Session } from "./types";

const SESSION_KEY = "groupmate.session";

export function loadSession(): Session | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    return raw ? (JSON.parse(raw) as Session) : null;
  } catch {
    return null;
  }
}

export function saveSession(session: Session | null) {
  try {
    if (session) localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    else localStorage.removeItem(SESSION_KEY);
  } catch {
    // localStorage var nebūt pieejams – sesija tad saglabājas tikai atmiņā.
  }
}

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

function headers(extra?: Record<string, string>): Record<string, string> {
  const session = loadSession();
  // Prototipa vienkāršojums: īstas autentifikācijas vietā tiek sūtīts izvēlētā lietotāja ID.
  return {
    ...(session ? { "X-User-Id": String(session.id) } : {}),
    ...extra,
  };
}

export async function api<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const res = await fetch(`/api${path}`, {
    method: options.method ?? "GET",
    headers: headers(options.body !== undefined ? { "Content-Type": "application/json" } : undefined),
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });

  const text = await res.text();
  let data: unknown = null;
  if (text) {
    try {
      data = JSON.parse(text);
    } catch {
      data = text;
    }
  }

  if (!res.ok) {
    const message =
      data && typeof data === "object" && "message" in data
        ? String((data as { message: unknown }).message)
        : `Servera kļūda (${res.status}).`;
    throw new ApiError(res.status, message);
  }
  return data as T;
}

/** Lejupielādē failu (piem., JSON eksportu), saglabājot autorizācijas galveni. */
export async function download(path: string, fileName: string) {
  const res = await fetch(`/api${path}`, { headers: headers() });
  if (!res.ok) throw new ApiError(res.status, "Neizdevās lejupielādēt failu.");
  const blob = await res.blob();
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}

export function errorMessage(e: unknown): string {
  if (e instanceof ApiError) return e.message;
  if (e instanceof TypeError) return "Nav savienojuma ar serveri. Pārbaudi, vai aizmugursistēma darbojas.";
  return "Notika neparedzēta kļūda.";
}

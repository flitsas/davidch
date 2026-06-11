import { cookies } from "next/headers";
import { ACCESS_COOKIE, REFRESH_COOKIE } from "./cookies";

const API_BASE = process.env.API_BASE_URL ?? "http://localhost:5080";

export class SessionRevokedError extends Error {
  constructor() {
    super("SESSION_REVOKED");
  }
}

export async function apiFetch(path: string, init: RequestInit = {}) {
  const jar = await cookies();
  const cookieHeader = jar
    .getAll()
    .filter((c) => c.name === ACCESS_COOKIE || c.name === REFRESH_COOKIE)
    .map((c) => `${c.name}=${c.value}`)
    .join("; ");

  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(cookieHeader ? { Cookie: cookieHeader } : {}),
      ...init.headers,
    },
    cache: "no-store",
  });

  if (res.status === 403) {
    const body = await res
      .clone()
      .json()
      .catch(() => ({}));
    if (body.code === "SESSION_REVOKED" || res.headers.get("X-Session-Revoked") === "true") {
      throw new SessionRevokedError();
    }
  }

  return res;
}

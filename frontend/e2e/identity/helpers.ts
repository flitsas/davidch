import { APIRequestContext } from "@playwright/test";

export async function isStackAvailable(baseURL: string): Promise<boolean> {
  try {
    const res = await fetch(`${baseURL}/api/health`, { signal: AbortSignal.timeout(3000) });
    return res.ok;
  } catch {
    return false;
  }
}

export async function fetchMailhogActivationLink(
  request: APIRequestContext,
  email: string
): Promise<string | null> {
  const mailhog = process.env.MAILHOG_URL ?? "http://localhost:8025";
  const res = await request.get(`${mailhog}/api/v2/search?kind=to&query=${encodeURIComponent(email)}`);
  if (!res.ok()) return null;
  const data = await res.json();
  const body = data?.items?.[0]?.Content?.Body as string | undefined;
  if (!body) return null;
  const match = body.match(/\/activate\?token=([^"\s&]+)/);
  return match?.[1] ?? null;
}

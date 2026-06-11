import { APIRequestContext, Page, expect } from "@playwright/test";

export const DEFAULT_ADMIN_EMAIL = "super@flit.local";
export const DEFAULT_ADMIN_PASSWORD = "FlitDev2026!";

export function adminCredentials() {
  return {
    email: process.env.E2E_ADMIN_EMAIL ?? DEFAULT_ADMIN_EMAIL,
    password: process.env.E2E_ADMIN_PASSWORD ?? DEFAULT_ADMIN_PASSWORD,
  };
}

export async function isStackAvailable(baseURL: string): Promise<boolean> {
  try {
    const res = await fetch(`${baseURL}/api/health`, { signal: AbortSignal.timeout(3000) });
    return res.ok;
  } catch {
    return false;
  }
}

export async function loginAsAdmin(page: Page) {
  const { email, password } = adminCredentials();
  await page.goto("/login");
  await page.getByLabel("Correo").fill(email);
  await page.getByLabel("Contraseña").fill(password);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes("/api/auth/login") && res.ok(), {
      timeout: 15_000,
    }),
    page.getByRole("button", { name: "Entrar" }).click(),
  ]);
  await page.waitForURL((url) => url.pathname === "/", { timeout: 15_000 });
  await expect(page.getByRole("heading", { name: "FLIT Identidad" })).toBeVisible();
}

export async function waitForMailhogToken(
  request: APIRequestContext,
  email: string,
  path: "/activate" | "/reset-password",
  attempts = 12,
): Promise<string | null> {
  const mailhog = process.env.MAILHOG_URL ?? "http://localhost:8025";
  const pattern =
    path === "/activate" ? /\/activate\?token=([^"\s&]+)/ : /\/reset-password\?token=([^"\s&]+)/;

  for (let i = 0; i < attempts; i++) {
    const res = await request.get(
      `${mailhog}/api/v2/search?kind=to&query=${encodeURIComponent(email)}`,
    );
    if (res.ok()) {
      const data = await res.json();
      const body = data?.items?.[0]?.Content?.Body as string | undefined;
      const match = body?.match(pattern);
      if (match?.[1]) return decodeURIComponent(match[1]);
    }
    await new Promise((r) => setTimeout(r, 500));
  }
  return null;
}

export async function fetchMailhogActivationLink(
  request: APIRequestContext,
  email: string,
): Promise<string | null> {
  return waitForMailhogToken(request, email, "/activate");
}

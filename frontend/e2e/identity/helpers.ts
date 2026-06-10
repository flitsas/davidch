import { APIRequestContext, Page } from "@playwright/test";

export async function isStackAvailable(baseURL: string): Promise<boolean> {
  try {
    const res = await fetch(`${baseURL}/api/health`, { signal: AbortSignal.timeout(3000) });
    return res.ok;
  } catch {
    return false;
  }
}

export async function loginAsAdmin(page: Page) {
  await page.goto("/login");
  await page.fill('input[type="email"]', process.env.E2E_ADMIN_EMAIL ?? "super@flit.local");
  await page.fill(
    'input[type="password"]',
    process.env.E2E_ADMIN_PASSWORD ?? "ChangeMe!123"
  );
  await page.click('button[type="submit"]');
  await page.waitForURL(/\//);
}

export async function waitForMailhogToken(
  request: APIRequestContext,
  email: string,
  path: "/activate" | "/reset-password",
  attempts = 12
): Promise<string | null> {
  const mailhog = process.env.MAILHOG_URL ?? "http://localhost:8025";
  const pattern =
    path === "/activate"
      ? /\/activate\?token=([^"\s&]+)/
      : /\/reset-password\?token=([^"\s&]+)/;

  for (let i = 0; i < attempts; i++) {
    const res = await request.get(
      `${mailhog}/api/v2/search?kind=to&query=${encodeURIComponent(email)}`
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
  email: string
): Promise<string | null> {
  return waitForMailhogToken(request, email, "/activate");
}

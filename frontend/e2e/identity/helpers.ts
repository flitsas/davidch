import { APIRequestContext, Page, expect, type TestInfo } from "@playwright/test";

export const DEFAULT_ADMIN_EMAIL = "super@flit.local";
export const DEFAULT_ADMIN_PASSWORD = "FlitDev2026!";
export const DEFAULT_TENANT_ADMIN_EMAIL = "admin@tenant-a.com";
export const DEFAULT_TENANT_ADMIN_PASSWORD = "SecurePass!123";

export function getPlaywrightBaseUrl() {
  return (
    process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${process.env.FRONTEND_PORT ?? "40102"}`
  );
}

export async function skipIfStackUnavailable(testInfo: TestInfo) {
  if (!(await isStackAvailable(getPlaywrightBaseUrl()))) {
    testInfo.skip(
      true,
      "Stack not running — start docker compose (default http://127.0.0.1:40102)",
    );
  }
}

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

export async function loginAsTenantAdmin(page: Page) {
  const email = process.env.E2E_TENANT_ADMIN_EMAIL ?? DEFAULT_TENANT_ADMIN_EMAIL;
  const password = process.env.E2E_TENANT_ADMIN_PASSWORD ?? DEFAULT_TENANT_ADMIN_PASSWORD;
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
}

/** Pointer drag compatible with @dnd-kit sortable handles. */
export async function dragHandleToHandle(page: Page, sourceTestId: string, targetTestId: string) {
  const source = page.getByTestId(sourceTestId);
  const target = page.getByTestId(targetTestId);
  await source.scrollIntoViewIfNeeded();
  await target.scrollIntoViewIfNeeded();
  const sourceBox = await source.boundingBox();
  const targetBox = await target.boundingBox();
  if (!sourceBox || !targetBox) {
    throw new Error(`Missing drag handle bounds: ${sourceTestId} -> ${targetTestId}`);
  }

  await page.mouse.move(sourceBox.x + sourceBox.width / 2, sourceBox.y + sourceBox.height / 2);
  await page.mouse.down();
  await page.mouse.move(targetBox.x + targetBox.width / 2, targetBox.y + targetBox.height / 2, {
    steps: 20,
  });
  await page.mouse.up();
}

const DEFAULT_MATRICULA_ORDER = [
  { document_type_code: "CEDULA", position: 1, is_included: true },
  { document_type_code: "TARJETA_PROPIEDAD", position: 2, is_included: true },
  { document_type_code: "SOAT", position: 3, is_included: true },
  { document_type_code: "RTM", position: 4, is_included: true },
  { document_type_code: "FACTURA", position: 5, is_included: true },
];

export async function resetMatriculaDocumentOrder(request: APIRequestContext) {
  const base = getPlaywrightBaseUrl();
  const login = await request.post(`${base}/api/auth/login`, {
    data: {
      email: process.env.E2E_TENANT_ADMIN_EMAIL ?? DEFAULT_TENANT_ADMIN_EMAIL,
      password: process.env.E2E_TENANT_ADMIN_PASSWORD ?? DEFAULT_TENANT_ADMIN_PASSWORD,
    },
  });
  expect(login.ok()).toBeTruthy();

  const put = await request.put(`${base}/api/v1/ot/settings/document-order/MATRICULA_INICIAL`, {
    data: { items: DEFAULT_MATRICULA_ORDER },
  });
  expect(put.ok()).toBeTruthy();
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

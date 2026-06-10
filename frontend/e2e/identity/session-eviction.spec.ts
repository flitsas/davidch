import { test, expect } from "@playwright/test";
import { isStackAvailable } from "./helpers";

const BASE = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? "super@flit.local";
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? "ChangeMe!123";

test.beforeEach(async ({}, testInfo) => {
  if (!(await isStackAvailable(BASE))) {
    testInfo.skip(true, "Stack not running");
  }
});

test("login page shows session revoked banner", async ({ page }) => {
  await page.goto("/login?reason=session_revoked");
  await expect(page.getByRole("alert")).toContainText(/sesión fue cerrada/i);
});

test("logout clears session", async ({ page, request }) => {
  const loginRes = await request.post(`${BASE}/api/auth/login`, {
    data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
  });
  expect(loginRes.ok()).toBeTruthy();

  const cookies = loginRes
    .headersArray()
    .filter((h) => h.name.toLowerCase() === "set-cookie")
    .map((h) => {
      const [pair] = h.value.split(";");
      const [name, value] = pair.split("=");
      return { name, value, url: BASE };
    });
  await page.context().addCookies(cookies);

  await page.goto("/");
  await expect(page.getByText("FLIT Identidad")).toBeVisible();

  await page.getByRole("button", { name: /cerrar sesión/i }).click();
  await expect(page).toHaveURL(/\/login/);

  const meRes = await request.get(`${BASE}/api/auth/me`, {
    headers: { Cookie: cookies.map((c) => `${c.name}=${c.value}`).join("; ") },
  });
  expect(meRes.status()).toBe(401);
});

import { test, expect } from "@playwright/test";
import { isStackAvailable, loginAsAdmin } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running");
  }
});

test("login page shows session revoked banner", async ({ page }) => {
  await page.goto("/login?reason=session_revoked");
  await expect(page.getByText(/sesión fue cerrada/i)).toBeVisible();
});

test("logout clears session", async ({ page, request }) => {
  await loginAsAdmin(page);

  await page.getByRole("button", { name: /cerrar sesión/i }).click();
  await expect(page).toHaveURL(/\/login/);

  const meRes = await request.get("/api/auth/me");
  expect(meRes.status()).toBe(401);
});

import { test, expect } from "@playwright/test";
import { isStackAvailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:5000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running — start docker, API, frontend, and gateway");
  }
});

test("login as super admin shows home", async ({ page }) => {
  await page.goto("/login");
  await page.fill('input[type="email"]', process.env.E2E_ADMIN_EMAIL ?? "super@flit.local");
  await page.fill(
    'input[type="password"]',
    process.env.E2E_ADMIN_PASSWORD ?? "ChangeMe!123"
  );
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/\//);
  await expect(page.getByText("FLIT Identidad")).toBeVisible();
});

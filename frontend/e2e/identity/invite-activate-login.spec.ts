import { test, expect } from "@playwright/test";
import { isStackAvailable, loginAsAdmin, waitForMailhogToken } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running — start docker compose");
  }
});

test("invite activate login flow", async ({ page, request }) => {
  const email = `e2e.${Date.now()}@tenant-a.com`;

  await loginAsAdmin(page);
  await page.goto("/admin/users");

  await page.fill('input[type="email"]', email);
  const roleCheckbox = page.locator('input[type="checkbox"]').first();
  if (await roleCheckbox.isVisible()) {
    await roleCheckbox.check();
  }
  await page.getByRole("button", { name: /invitación/i }).click();

  const token = await waitForMailhogToken(request, email, "/activate");
  expect(token).toBeTruthy();

  await page.goto(`/activate?token=${encodeURIComponent(token!)}`);
  await page.fill('input[type="password"]', "SecurePass!123");
  await page.click('button[type="submit"]');

  await page.goto("/login");
  await page.fill('input[type="email"]', email);
  await page.fill('input[type="password"]', "SecurePass!123");
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL(/\//);
  await expect(page.getByText("FLIT Identidad")).toBeVisible();
});

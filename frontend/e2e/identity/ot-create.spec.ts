import { test, expect } from "@playwright/test";
import { isStackAvailable, loginAsAdmin } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running — start docker compose");
  }
});

test("super admin can open OT create wizard", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/ot/new");
  await expect(page.getByRole("heading", { name: "Nuevo organismo de tránsito" })).toBeVisible();
  await expect(page.getByLabel("Código DIVIPOL")).toBeVisible();
  await expect(page.getByRole("button", { name: "Crear OT" })).toBeVisible();
});

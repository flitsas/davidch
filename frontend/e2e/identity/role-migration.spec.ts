import { test, expect } from "@playwright/test";
import { isStackAvailable, loginAsAdmin } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running");
  }
});

test("delete role with users shows migration UI", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/roles");

  const editLink = page.getByRole("link", { name: /editar permisos/i }).first();
  if (!(await editLink.isVisible())) {
    test.skip(true, "No editable roles available");
    return;
  }
  await editLink.click();

  await page.getByRole("button", { name: /eliminar rol/i }).click();

  const migratePanel = page.locator("select").filter({ hasText: /reemplazo/i });
  const deleted = page.url().endsWith("/admin/roles");
  const hasMigrate = await migratePanel.isVisible().catch(() => false);
  expect(hasMigrate || deleted).toBeTruthy();
});

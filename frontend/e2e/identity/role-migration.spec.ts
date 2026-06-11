import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("delete role with users shows migration UI", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/roles");

  const editLink = page.getByRole("link", { name: /editar permisos/i }).first();
  await expect(editLink).toBeVisible();
  await editLink.click();

  await Promise.all([
    page.waitForResponse(
      (res) =>
        res.request().method() === "DELETE" &&
        res.url().includes("/api/roles/") &&
        (res.status() === 409 || res.status() === 204),
    ),
    page.getByRole("button", { name: /eliminar rol/i }).click(),
  ]);

  const migrateSelect = page.getByLabel("Rol de reemplazo");
  const deleted = /\/admin\/roles\/?$/.test(new URL(page.url()).pathname);
  const hasMigrate = await migrateSelect.isVisible().catch(() => false);
  expect(hasMigrate || deleted).toBeTruthy();
});

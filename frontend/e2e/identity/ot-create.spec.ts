import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("super admin can open OT create wizard", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/ot/new");
  await expect(page.getByRole("heading", { name: "Nuevo organismo de tránsito" })).toBeVisible();
  await expect(page.getByLabel("Código DIVIPOL")).toBeVisible();
  await expect(page.getByRole("button", { name: "Crear OT" })).toBeVisible();
});

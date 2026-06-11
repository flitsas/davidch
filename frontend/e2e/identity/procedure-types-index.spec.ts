import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("super admin can open procedure types index", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/procedure-types");
  await expect(page.getByRole("heading", { name: "Tipos de trámite" })).toBeVisible();
  await expect(page.getByTestId("procedure-types-index")).toBeVisible();
  await expect(page.getByTestId("procedure-type-new")).toBeVisible();
});

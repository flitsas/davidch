import { test, expect } from "@playwright/test";
import { loginAsTenantAdmin, skipIfStackUnavailable } from "../identity/helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("tenant admin can open tramites index", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await expect(page.getByRole("link", { name: "Trámites" })).toBeVisible();
  await page.goto("/tramites");
  await expect(page.getByRole("heading", { name: "Trámites" })).toBeVisible();
  await expect(page.getByTestId("tramites-index")).toBeVisible();
  const empty = page.getByTestId("tramites-empty");
  const table = page.getByTestId("tramites-index-table");
  await expect(empty.or(table)).toBeVisible();
});

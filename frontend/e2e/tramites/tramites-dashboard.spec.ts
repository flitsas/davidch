import { test, expect } from "@playwright/test";
import { loginAsTenantAdmin, skipIfStackUnavailable } from "../identity/helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("tenant admin can open dashboard and see chart", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await expect(page.getByRole("link", { name: "Dashboard" })).toBeVisible();
  await page.goto("/tramites/dashboard");
  await expect(page.getByTestId("tramites-dashboard")).toBeVisible();
  await expect(page.getByTestId("dashboard-donut-chart")).toBeVisible();
});

test("clicking chart segment shows detail table when data exists", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await page.goto("/tramites/dashboard");
  await expect(page.getByTestId("tramites-dashboard")).toBeVisible();

  const chart = page.getByTestId("dashboard-donut-chart");
  const empty = page.getByText("No hay trámites en el rango seleccionado.");
  await expect(chart.or(empty)).toBeVisible({ timeout: 15_000 });

  if (await empty.isVisible()) {
    test.skip(true, "No tramites in date range");
    return;
  }

  const legend = page
    .getByRole("button", { name: /Matrículas|Traspasos|Otros/i })
    .filter({ hasText: /\([1-9]/ })
    .first();
  await legend.click();
  await expect(page.getByTestId("dashboard-detail-table")).toBeVisible();
});

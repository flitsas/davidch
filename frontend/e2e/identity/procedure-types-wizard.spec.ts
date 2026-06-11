import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("super admin can create and edit a procedure type via wizard", async ({ page }) => {
  test.setTimeout(90_000);

  const uniqueName = `E2E Trámite ${Date.now()}`;

  await loginAsAdmin(page);
  await page.goto("/admin/procedure-types/new");
  await expect(page.getByTestId("procedure-type-wizard")).toBeVisible();

  await page.getByTestId("procedure-name-input").fill(uniqueName);
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("vehicle-mode-vin").check();
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("actor-input").fill("Vendedor");
  await page.getByTestId("add-actor-button").click();
  await expect(page.getByTestId("actors-list")).toContainText("Vendedor");
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("document-label-input").fill("Escritura pública");
  await page.getByTestId("add-document-button").click();
  await expect(page.getByTestId("documents-list")).toContainText("Escritura pública");

  const createResponse = page.waitForResponse(
    (res) =>
      res.url().includes("/api/v1/admin/procedure-types") && res.request().method() === "POST",
    { timeout: 15_000 },
  );
  await page.getByTestId("wizard-save").click();
  const response = await createResponse;
  expect(response.ok(), `Create failed HTTP ${response.status()}`).toBeTruthy();

  await expect(page).toHaveURL(/\/admin\/procedure-types$/);
  await expect(page.getByText(uniqueName)).toBeVisible();

  await page
    .getByRole("row", { name: new RegExp(uniqueName) })
    .getByRole("link", { name: "Editar" })
    .click();
  await expect(page.getByTestId("procedure-type-wizard")).toBeVisible();
  await page.getByTestId("wizard-next").click();
  await expect(page.getByTestId("vehicle-mode-vin")).toBeChecked();
});

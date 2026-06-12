import { test, expect, type APIRequestContext } from "@playwright/test";
import {
  adminCredentials,
  getPlaywrightBaseUrl,
  loginAsTenantAdmin,
  skipIfStackUnavailable,
} from "../identity/helpers";
import fs from "node:fs";
import os from "node:os";
import path from "node:path";

async function createProcedureTypeForE2E(request: APIRequestContext): Promise<string> {
  const base = getPlaywrightBaseUrl();
  const { email, password } = adminCredentials();
  const login = await request.post(`${base}/api/auth/login`, {
    data: { email, password },
  });
  expect(login.ok()).toBeTruthy();

  const uniqueName = `E2E Tramite ${Date.now()}`;
  const create = await request.post(`${base}/api/v1/admin/procedure-types`, {
    data: {
      name: uniqueName,
      vehicleQueryMode: "Plate",
      actors: [{ roleLabel: "Vendedor" }, { roleLabel: "Comprador" }],
      documents: [{ label: "Escritura", kind: "Static" }],
    },
  });
  expect(create.ok()).toBeTruthy();
  const body = await create.json();
  return body.id as string;
}

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("tenant admin can create tramite via wizard", async ({ page, request }) => {
  test.setTimeout(120_000);

  const typeId = await createProcedureTypeForE2E(request);
  await loginAsTenantAdmin(page);
  await page.goto("/tramites");

  await page.getByTestId("tramites-new").click();
  await expect(page.getByTestId("tramite-wizard")).toBeVisible();

  await page.getByTestId("procedure-type-select").selectOption(typeId);
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("ot-option-11001000").click();
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("vehicle-query-input").fill("ABC123");
  await page.getByTestId("wizard-next").click();

  await page.getByTestId("actor-doc-number-1").fill("1234567890");
  await page.getByTestId("actor-doc-number-2").fill("0987654321");
  await page.getByTestId("wizard-next").click();

  const pdfPath = path.join(os.tmpdir(), `tramite-e2e-${Date.now()}.pdf`);
  fs.writeFileSync(pdfPath, "%PDF-1.4\n%EOF\n");
  await page.getByTestId("document-upload-0").locator('input[type="file"]').setInputFiles(pdfPath);
  await page.getByTestId("wizard-next").click();

  const createResponse = page.waitForResponse(
    (res) => res.url().includes("/api/v1/tramites") && res.request().method() === "POST",
    { timeout: 20_000 },
  );
  await page.getByTestId("wizard-submit").click();
  const response = await createResponse;
  expect(response.ok(), `Create failed HTTP ${response.status()}`).toBeTruthy();

  await expect(page.getByTestId("tramites-index-table")).toBeVisible();
  await expect(page.getByText("ABC123")).toBeVisible();
});

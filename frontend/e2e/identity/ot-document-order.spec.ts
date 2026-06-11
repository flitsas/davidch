import { test, expect } from "@playwright/test";
import {
  dragHandleToHandle,
  loginAsAdmin,
  loginAsTenantAdmin,
  resetMatriculaDocumentOrder,
  skipIfStackUnavailable,
} from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("tenant admin can reorder documents and persist after reload", async ({ page, request }) => {
  test.setTimeout(90_000);

  await resetMatriculaDocumentOrder(request);
  await loginAsTenantAdmin(page);
  await page.goto("/ot/settings/documentos");
  await expect(page.getByRole("heading", { name: "Orden de documentos" })).toBeVisible();
  await expect(page.getByTestId("document-order-list")).toBeVisible();
  await expect(page.getByTestId("doc-row-CEDULA").getByText("1", { exact: true })).toBeVisible();

  await expect(async () => {
    await dragHandleToHandle(page, "doc-handle-TARJETA_PROPIEDAD", "doc-handle-CEDULA");
    await expect(page.getByTestId("doc-row-TARJETA_PROPIEDAD").getByText("1", { exact: true })).toBeVisible({
      timeout: 2_000,
    });
  }).toPass({ timeout: 15_000 });

  await page.waitForTimeout(500);

  const saveResponse = page.waitForResponse(
    (res) =>
      res.url().includes("/api/v1/ot/settings/document-order/MATRICULA_INICIAL") &&
      res.request().method() === "PUT",
    { timeout: 15_000 },
  );
  await page.getByRole("button", { name: "Guardar orden de documentos" }).click();
  const response = await saveResponse;
  expect(response.ok(), `Save failed with HTTP ${response.status()}`).toBeTruthy();

  const saved = (await response.json()) as {
    items: { document_type_code: string; position: number }[];
  };
  expect(saved.items.find((item) => item.document_type_code === "TARJETA_PROPIEDAD")?.position).toBe(1);

  await page.reload();
  await expect(page.getByTestId("doc-row-TARJETA_PROPIEDAD").getByText("1", { exact: true })).toBeVisible();
  await expect(page.getByTestId("doc-row-CEDULA").getByText("2", { exact: true })).toBeVisible();
});

test("super admin can open document order tab", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/ot");
  await page.getByRole("link", { name: "Configurar" }).first().click();
  await page.getByRole("link", { name: "Documentos" }).click();
  await expect(page.getByTestId("procedure-type-select")).toBeVisible();
  await expect(page.getByTestId("document-order-list")).toBeVisible();
});

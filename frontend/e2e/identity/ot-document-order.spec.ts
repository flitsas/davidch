import { test, expect } from "@playwright/test";
import { isStackAvailable, loginAsAdmin } from "./helpers";

const TENANT_ADMIN_EMAIL = process.env.E2E_TENANT_ADMIN_EMAIL ?? "admin@tenant-a.com";
const TENANT_ADMIN_PASSWORD = process.env.E2E_TENANT_ADMIN_PASSWORD ?? "SecurePass!123";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running — start docker compose");
  }
});

async function loginAsTenantAdmin(page: import("@playwright/test").Page) {
  await page.goto("/login");
  await page.getByLabel("Correo").fill(TENANT_ADMIN_EMAIL);
  await page.getByLabel("Contraseña").fill(TENANT_ADMIN_PASSWORD);
  await Promise.all([
    page.waitForResponse((res) => res.url().includes("/api/auth/login") && res.ok(), {
      timeout: 15_000,
    }),
    page.getByRole("button", { name: "Entrar" }).click(),
  ]);
  await page.waitForURL((url) => url.pathname === "/", { timeout: 15_000 });
}

test("tenant admin can reorder documents and persist after reload", async ({ page }) => {
  await loginAsTenantAdmin(page);
  await page.goto("/ot/settings/documentos");
  await expect(page.getByRole("heading", { name: "Orden de documentos" })).toBeVisible();
  await expect(page.getByTestId("document-order-list")).toBeVisible();

  const firstRow = page.getByTestId("doc-row-CEDULA");
  const secondRow = page.getByTestId("doc-row-TARJETA_PROPIEDAD");
  await expect(firstRow).toBeVisible();
  await expect(secondRow).toBeVisible();

  await page.getByTestId("doc-handle-TARJETA_PROPIEDAD").dragTo(page.getByTestId("doc-handle-CEDULA"));

  await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes("/api/v1/ot/settings/document-order/MATRICULA_INICIAL") &&
        res.request().method() === "PUT" &&
        res.ok(),
      { timeout: 15_000 },
    ),
    page.getByRole("button", { name: "Guardar orden de documentos" }).click(),
  ]);

  await page.reload();
  await expect(page.getByTestId("doc-row-TARJETA_PROPIEDAD").locator("span").first()).toHaveText("1");
  await expect(page.getByTestId("doc-row-CEDULA").locator("span").first()).toHaveText("2");
});

test("super admin can open document order tab", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/ot");
  await page.getByRole("link", { name: "Configurar" }).first().click();
  await page.getByRole("link", { name: "Documentos" }).click();
  await expect(page.getByTestId("procedure-type-select")).toBeVisible();
  await expect(page.getByTestId("document-order-list")).toBeVisible();
});

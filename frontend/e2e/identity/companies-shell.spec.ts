import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("super admin can create company and open tab shell", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/companies/new");

  const slug = `e2e-${Date.now()}`;
  await page.getByLabel("NIT").fill(`900${Date.now()}`.slice(0, 12));
  await page.getByLabel("Razón social").fill("E2E Company SAS");
  await page.getByLabel("Slug del tenant").fill(slug);

  await Promise.all([
    page.waitForResponse(
      (res) =>
        res.url().includes("/api/v1/admin/companies") &&
        res.request().method() === "POST" &&
        res.ok(),
      { timeout: 15_000 },
    ),
    page.getByRole("button", { name: "Crear compañía" }).click(),
  ]);

  await expect(page).toHaveURL(/\/admin\/companies\/[0-9a-f-]+\?tab=config-empresa/);
  await expect(page.getByRole("navigation", { name: "Secciones de configuración" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Matrícula" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Traspasos" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Config Empresa" })).toBeVisible();
  await expect(page.getByRole("link", { name: "Contingencia" })).toBeVisible();
});

test("tenant admin sees access denied on company detail", async ({ page }) => {
  await page.goto("/login");
  await page.getByLabel("Correo").fill("admin@tenant-a.com");
  await page.getByLabel("Contraseña").fill("SecurePass!123");
  await Promise.all([
    page.waitForResponse((res) => res.url().includes("/api/auth/login") && res.ok(), {
      timeout: 15_000,
    }),
    page.getByRole("button", { name: "Entrar" }).click(),
  ]);
  await page.waitForURL((url) => url.pathname === "/", { timeout: 15_000 });

  await page.goto("/admin/companies/00000000-0000-0000-0000-000000000001");
  await expect(page.getByRole("heading", { name: "Acceso denegado" })).toBeVisible();
});

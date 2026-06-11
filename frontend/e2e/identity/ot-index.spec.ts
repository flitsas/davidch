import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("super admin can open OT index", async ({ page }) => {
  await loginAsAdmin(page);
  await page.goto("/admin/ot");
  await expect(page.getByRole("heading", { name: "Organismos de tránsito" })).toBeVisible();
  await expect(page.getByRole("heading", { name: "Filtros" })).toBeVisible();
});

test("tenant admin sees access denied on OT index", async ({ page }) => {
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

  await page.goto("/admin/ot");
  await expect(page.getByRole("heading", { name: "Acceso denegado" })).toBeVisible();
});

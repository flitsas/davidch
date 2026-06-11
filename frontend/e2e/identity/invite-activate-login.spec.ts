import { test, expect } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable, waitForMailhogToken } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("invite activate login flow", async ({ page, request }) => {
  const email = `e2e.${Date.now()}@tenant-a.com`;

  await loginAsAdmin(page);
  await page.goto("/admin/users");

  const tenantSelect = page.locator('select[name="tenant"]');
  if (await tenantSelect.isVisible()) {
    await tenantSelect.selectOption({ label: "Tenant A" });
  }

  await page.locator("#invite-email").fill(email);
  await page.getByRole("group", { name: "Roles" }).getByText("TenantA-Operator").click();
  await page.getByRole("button", { name: /enviar invitación/i }).click();

  const token = await waitForMailhogToken(request, email, "/activate");
  expect(token).toBeTruthy();

  await page.goto(`/activate?token=${encodeURIComponent(token!)}`);
  await page.getByLabel("Nueva contraseña").fill("SecurePass!123");
  await page.getByLabel("Confirmar contraseña").fill("SecurePass!123");
  await page.getByRole("button", { name: "Activar cuenta" }).click();
  await expect(page).toHaveURL(/\/login/);

  await page.goto("/login");
  await page.getByLabel("Correo").fill(email);
  await page.getByLabel("Contraseña").fill("SecurePass!123");
  await page.getByRole("button", { name: "Entrar" }).click();
  await expect(page.getByRole("heading", { name: "FLIT Identidad" })).toBeVisible();
});

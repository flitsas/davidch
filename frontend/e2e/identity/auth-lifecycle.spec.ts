import { test } from "@playwright/test";
import { isStackAvailable, loginAsAdmin } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  const base = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000";
  if (!(await isStackAvailable(base))) {
    testInfo.skip(true, "Stack not running — start docker compose");
  }
});

test("login as super admin shows home", async ({ page }) => {
  await loginAsAdmin(page);
});

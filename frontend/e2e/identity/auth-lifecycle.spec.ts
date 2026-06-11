import { test } from "@playwright/test";
import { loginAsAdmin, skipIfStackUnavailable } from "./helpers";

test.beforeEach(async ({}, testInfo) => {
  await skipIfStackUnavailable(testInfo);
});

test("login as super admin shows home", async ({ page }) => {
  await loginAsAdmin(page);
});

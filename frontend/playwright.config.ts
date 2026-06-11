import { defineConfig } from "@playwright/test";

const defaultBaseUrl =
  process.env.PLAYWRIGHT_BASE_URL ?? `http://127.0.0.1:${process.env.FRONTEND_PORT ?? "40102"}`;

export default defineConfig({
  testDir: "./e2e",
  timeout: 60_000,
  workers: process.env.CI ? 1 : undefined,
  use: {
    baseURL: defaultBaseUrl,
    trace: "on-first-retry",
  },
});

import { defineConfig, devices } from '@playwright/test'

// Runs against an already running stack (AGENTS.md: "requires running stack").
// E2E_BASE_URL: the Api serving the built SPA - Docker Compose in CI, `dotnet run` locally.
export default defineConfig({
  testDir: '.',
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5083',
    trace: 'retain-on-failure',
  },
  // scope_contract: desktop Chrome and a 360 px wide mobile view.
  projects: [
    { name: 'desktop-chrome', use: { ...devices['Desktop Chrome'] } },
    { name: 'mobile-360', use: { ...devices['Desktop Chrome'], viewport: { width: 360, height: 740 }, isMobile: true, hasTouch: true } },
  ],
})

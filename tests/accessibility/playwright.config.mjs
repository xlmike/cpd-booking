import { defineConfig, devices } from '@playwright/test'

const baseURL = process.env.BASE_URL ?? 'http://127.0.0.1:5080'

export default defineConfig({
  testDir: '.',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: { baseURL, trace: 'retain-on-failure' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  // Starts the app (in-memory data) unless BASE_URL points at a running environment.
  webServer: process.env.BASE_URL
    ? undefined
    : {
        command: 'dotnet run --project src/CpdBooking.Web --no-launch-profile --urls http://127.0.0.1:5080',
        cwd: '../..',
        url: `${baseURL}/health`,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
        env: { ASPNETCORE_ENVIRONMENT: 'Development' }
      }
})

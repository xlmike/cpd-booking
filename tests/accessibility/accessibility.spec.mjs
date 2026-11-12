import { test, expect } from '@playwright/test'
import AxeBuilder from '@axe-core/playwright'

// Automated checks catch around a third of WCAG issues. Keep doing manual keyboard and screen reader tests.
const WCAG_22_AA = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']
const COURSE = '/courses/7d3c1c8e-3b6a-4f53-9a51-1f0f3b1e2a02'

async function expectNoViolations (page) {
  const results = await new AxeBuilder({ page }).withTags(WCAG_22_AA).analyze()
  expect(results.violations, JSON.stringify(results.violations, null, 2)).toEqual([])
}

test.describe('pages meet WCAG 2.2 AA (automated checks)', () => {
  for (const path of ['/', COURSE, `${COURSE}/book`, '/accessibility', '/page-that-does-not-exist']) {
    test(`no axe violations on ${path}`, async ({ page }) => {
      await page.goto(path)
      await expectNoViolations(page)
    })
  }
})

test('the error page is accessible and focuses the error summary', async ({ page }) => {
  await page.goto(`${COURSE}/book`)
  await page.getByRole('button', { name: 'Confirm booking' }).click()

  await expect(page).toHaveTitle(/^Error: /)
  const summary = page.locator('.govuk-error-summary')
  await expect(summary).toBeFocused()
  await expect(summary.getByRole('link', { name: 'Enter your email address' })).toHaveAttribute('href', '#Email')
  await expect(page.getByLabel('What is your email address?')).toHaveAttribute('aria-describedby', 'Email-hint Email-error')
  await expectNoViolations(page)
})

test('the skip link is the first thing a keyboard user reaches', async ({ page }) => {
  await page.goto('/')
  await page.keyboard.press('Tab')
  await expect(page.getByRole('link', { name: 'Skip to main content' })).toBeFocused()
})

test('a user can book a place using only the keyboard', async ({ page }) => {
  const email = `keyboard-${Date.now()}@example.com`
  await page.goto(COURSE)
  await page.getByRole('button', { name: 'Book a place' }).focus()
  await page.keyboard.press('Enter')
  await page.getByLabel('What is your email address?').fill(email)
  await page.keyboard.press('Enter')

  await expect(page.getByRole('heading', { level: 1, name: 'Booking confirmed' })).toBeVisible()
  await expect(page.locator('.govuk-panel__body strong')).toHaveText(/^CPD-[A-Z0-9]{4}-[A-Z0-9]{4}$/)
  await expectNoViolations(page)
})

test('filtering announces the number of results', async ({ page }) => {
  await page.goto('/')
  await page.getByLabel('Search by course or provider').fill('phonics')
  await expect(page.getByRole('status')).toHaveText('1 course found')
})

test('search still works without JavaScript', async ({ browser }) => {
  const context = await browser.newContext({ javaScriptEnabled: false })
  const page = await context.newPage()
  await page.goto('/')
  await page.getByLabel('Search by course or provider').fill('computing')
  await page.getByRole('button', { name: 'Search' }).click()
  await expect(page.getByRole('status')).toHaveText('1 course found')
  await context.close()
})

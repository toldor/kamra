import { expect, test, type Page } from '@playwright/test'

// ui_polish_minispec S1: the frame in a real browser - the band stays on top, on a phone the menu sits at the bottom,
// a focused control is never hidden under them (WCAG 2.4.11, Ü7), the Profilom panel and the tab icon.

const password = 'correct horse battery staple'

async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

async function expectNoHorizontalScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
  expect(overflow, 'horizontal overflow in px').toBeLessThanOrEqual(0)
}

// A long page, so there is something to scroll.
const makeLong = (page: Page) => page.evaluate(() => { document.querySelector('main')!.style.minHeight = '3000px' })

test('the band stays on top while scrolling', async ({ page }) => {
  await registerFreshHousehold(page)
  await makeLong(page)

  await page.evaluate(() => window.scrollTo(0, 1500))

  const band = await page.locator('header.band').boundingBox()
  expect(band?.y).toBe(0)
  await expect(page.getByRole('button', { name: 'Profilom' })).toBeInViewport()
})

test('on a phone the band is one row and the menu is a bar at the bottom', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'mobile-360', 'phone layout only')
  await registerFreshHousehold(page)

  const band = (await page.locator('header.band').boundingBox())!
  const nav = (await page.getByRole('navigation', { name: 'Oldalak' }).boundingBox())!
  const viewport = page.viewportSize()!
  expect(band.height).toBeLessThanOrEqual(64)
  expect(Math.round(nav.y + nav.height)).toBe(viewport.height)
  for (const name of ['Készlet', 'Mit főzzek?', 'Receptek', 'Bevásárlólista', 'Kérdezz']) {
    const link = (await page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name, exact: true }).boundingBox())!
    expect(link.y, name).toBeGreaterThanOrEqual(nav.y)
    expect(link.height, name).toBeGreaterThanOrEqual(44)
  }
  await expectNoHorizontalScroll(page)
})

// Mutation (2026-11-17): focusing a field from far away centers it, so a test that only focuses one passed without the
// padding too. The page scrolls a focused control clear of the band and the bar by its scroll padding, so that is
// what is measured: at least as tall as the band, and on a phone as the bar.
test('a focused control is not hidden under the band or the menu bar', async ({ page }) => {
  await registerFreshHousehold(page)

  const padding = await page.evaluate(() => {
    const style = getComputedStyle(document.documentElement)
    return { top: parseFloat(style.scrollPaddingTop), bottom: parseFloat(style.scrollPaddingBottom) }
  })
  const band = (await page.locator('header.band').boundingBox())!
  const nav = (await page.getByRole('navigation', { name: 'Oldalak' }).boundingBox())!
  expect(padding.top).toBeGreaterThanOrEqual(band.height)
  // On a phone the menu is a bar at the bottom of the screen.
  if (nav.y > band.y + band.height) expect(padding.bottom).toBeGreaterThanOrEqual(nav.height)
})

// Review R2: between 760 and about 1000 px the top menu wrapped into 2-3 rows (158 px). Below 1024 px the menu is the
// bar at the bottom, so the band stays one row on a tablet too.
test('on a tablet (900 px) the band is one row and the menu is the bar at the bottom', async ({ page }) => {
  await page.setViewportSize({ width: 900, height: 700 })
  await registerFreshHousehold(page)

  const band = (await page.locator('header.band').boundingBox())!
  const nav = (await page.getByRole('navigation', { name: 'Oldalak' }).boundingBox())!
  const padding = await page.evaluate(() => parseFloat(getComputedStyle(document.documentElement).scrollPaddingBottom))
  expect(band.height).toBeLessThanOrEqual(64)
  expect(Math.round(nav.y + nav.height)).toBe(700)
  expect(padding).toBeGreaterThanOrEqual(nav.height)
  await expectNoHorizontalScroll(page)
})

test('signs out from the Profilom panel, which Escape closes', async ({ page }) => {
  await registerFreshHousehold(page)
  const button = page.getByRole('button', { name: 'Profilom' })

  await button.click()
  await expect(page.getByText(/^Bejelentkezve: e2e-/)).toBeVisible()
  await page.keyboard.press('Escape')
  await expect(button).toHaveAttribute('aria-expanded', 'false')
  await expect(button).toBeFocused()

  await button.click()
  await page.getByRole('button', { name: 'Kijelentkezés' }).click()
  await expect(page.getByRole('heading', { name: 'Bejelentkezés' })).toBeVisible()
})

test('the tab icon is the Kamra jar, and the page tells the browser both colour schemes', async ({ page }) => {
  await page.goto('/')
  // Review T5: a dark system gets a dark page before the app's script runs.
  await expect(page.locator('meta[name="color-scheme"]')).toHaveAttribute('content', 'light dark')
  const href = await page.locator('link[rel="icon"]').getAttribute('href')
  const icon = await (await page.request.get(href!)).text()

  expect(icon).toContain('#f4f1e8')
  expect(icon).not.toContain('#863bff')
})

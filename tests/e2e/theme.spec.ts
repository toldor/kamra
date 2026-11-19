import { expect, test, type Page } from '@playwright/test'

// ui_polish_minispec S3 in a real browser: the dark palette follows the system, a choice overrides it and stays.

const password = 'correct horse battery staple'

async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

const ground = (page: Page) => page.evaluate(() => getComputedStyle(document.body).backgroundColor)

test('follows a dark system, and "Világos" overrides it, also after a reload', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
  await registerFreshHousehold(page)
  expect(await ground(page)).toBe('rgb(22, 26, 18)')

  await page.getByRole('button', { name: 'Profilom' }).click()
  await page.getByRole('region', { name: 'Profilom' }).getByRole('radio', { name: 'Világos' }).check()
  expect(await ground(page)).toBe('rgb(244, 245, 240)')

  await page.reload()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
  expect(await ground(page)).toBe('rgb(244, 245, 240)')
})

// Regression (the developer, 2026-11-17): in the dark theme a hovered menu item took the surface colour, which is dark,
// so it nearly vanished on the dark band. Desktop only: on a phone the bar has no hover.
test('a hovered menu item stays readable on the band in the dark theme', async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'desktop-chrome', 'hover on the top menu')
  await page.emulateMedia({ colorScheme: 'dark' })
  await registerFreshHousehold(page)
  const link = page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name: 'Receptek', exact: true })

  await link.hover()

  const ratio = await link.evaluate((a) => {
    const lum = (value: string) => {
      const [r, g, b] = value.match(/\d+/g)!.slice(0, 3).map(Number).map((c) => {
        const s = c / 255
        return s <= 0.04045 ? s / 12.92 : ((s + 0.055) / 1.055) ** 2.4
      })
      return 0.2126 * r + 0.7152 * g + 0.0722 * b
    }
    const text = lum(getComputedStyle(a).color)
    const band = lum(getComputedStyle(a.closest('header')!).backgroundColor)
    return (Math.max(text, band) + 0.05) / (Math.min(text, band) + 0.05)
  })
  expect(ratio).toBeGreaterThanOrEqual(4.5)
})

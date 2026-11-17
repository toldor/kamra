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

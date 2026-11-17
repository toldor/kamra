import { expect, test, type Page } from '@playwright/test'

// ui_polish_minispec S2 in a real browser: the printed shopping list and reduced motion.

const password = 'correct horse battery staple'

async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

test('the printed shopping list has no band, menu or buttons', async ({ page }) => {
  await registerFreshHousehold(page)
  await page.getByRole('link', { name: 'Bevásárlólista', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Nyomtatás' })).toBeVisible()

  await page.emulateMedia({ media: 'print' })

  await expect(page.getByRole('heading', { level: 1, name: 'Bevásárlólista' })).toBeVisible()
  await expect(page.locator('header.band')).toBeHidden()
  await expect(page.getByRole('navigation', { name: 'Oldalak' })).toBeHidden()
  await expect(page.getByRole('button', { name: 'Nyomtatás' })).toBeHidden()
})

test('the Profilom panel opens with a short animation, and without one when motion is reduced', async ({ page }) => {
  await registerFreshHousehold(page)
  const animation = () => page.locator('.profile-panel').evaluate((panel) => getComputedStyle(panel).animationName)

  await page.getByRole('button', { name: 'Profilom' }).click()
  expect(await animation()).not.toBe('none')
  await page.keyboard.press('Escape')

  await page.emulateMedia({ reducedMotion: 'reduce' })
  await page.getByRole('button', { name: 'Profilom' }).click()
  expect(await animation()).toBe('none')
})

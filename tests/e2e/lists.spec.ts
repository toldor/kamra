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
  // Review V5: a printed pantry has no entry card headings and no summary either.
  await page.emulateMedia({ media: 'print' })
  await expect(page.locator('details.card > summary').first()).toBeHidden()
  await page.emulateMedia({ media: 'screen' })
  await page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name: 'Bevásárlólista', exact: true }).click()
  await expect(page.getByRole('button', { name: 'Nyomtatás' })).toBeVisible()

  await page.emulateMedia({ media: 'print' })

  await expect(page.getByRole('heading', { level: 1, name: 'Bevásárlólista' })).toBeVisible()
  await expect(page.locator('header.band')).toBeHidden()
  await expect(page.getByRole('navigation', { name: 'Oldalak' })).toBeHidden()
  await expect(page.getByRole('button', { name: 'Nyomtatás' })).toBeHidden()
})

// Review T1: in the dark theme the printed list is dark text on white paper, not the screen's light text.
test('the shopping list prints dark on light in the dark theme too', async ({ page }) => {
  await page.emulateMedia({ colorScheme: 'dark' })
  await registerFreshHousehold(page)
  await page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name: 'Bevásárlólista', exact: true }).click()
  const heading = page.getByRole('heading', { level: 1, name: 'Bevásárlólista' })
  await expect(heading).toBeVisible()

  await page.emulateMedia({ media: 'print', colorScheme: 'dark' })

  const colors = await heading.evaluate((h) => ({
    text: getComputedStyle(h).color,
    page: getComputedStyle(document.body).backgroundColor,
    scheme: getComputedStyle(document.documentElement).colorScheme,
  }))
  expect(colors).toEqual({ text: 'rgb(30, 36, 25)', page: 'rgb(255, 255, 255)', scheme: 'light' })
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

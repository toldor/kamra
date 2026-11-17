import { expect, test, type Page } from '@playwright/test'

// ui_polish_minispec S5 (A2, A3, A6, A8, A11) on a desktop screen; on a phone everything stays in one column.

const password = 'correct horse battery staple'

async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

const menu = (page: Page, name: string) => page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name, exact: true })

test.beforeEach(({}, testInfo) => {
  test.skip(testInfo.project.name !== 'desktop-chrome', 'desktop layout only')
})

test('the pantry filters sit on top of the list, and the side column stays in view', async ({ page }) => {
  await registerFreshHousehold(page)

  const search = (await page.getByRole('search').boundingBox())!
  const list = (await page.getByRole('region', { name: 'Tételek' }).boundingBox())!
  expect(search.x).toBeGreaterThanOrEqual(list.x)
  expect(search.y).toBeGreaterThanOrEqual(list.y)
  expect(await page.locator('.side').evaluate((side) => getComputedStyle(side).position)).toBe('sticky')
})

test('the recipes are a grid of three cards in a row at 1280 px', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 800 })
  await registerFreshHousehold(page)
  await menu(page, 'Receptek').click()

  const cards = page.getByRole('article')
  await expect(cards.first()).toBeVisible()
  const boxes = await Promise.all([0, 1, 2, 3].map(async (i) => (await cards.nth(i).boundingBox())!))
  expect(boxes[1].y).toBe(boxes[0].y)
  expect(boxes[2].y).toBe(boxes[0].y)
  expect(boxes[1].x).toBeGreaterThan(boxes[0].x)
  expect(boxes[3].y).toBeGreaterThan(boxes[0].y)
})

test('the shopping list and its suggestions stand side by side', async ({ page }) => {
  await registerFreshHousehold(page)
  await menu(page, 'Bevásárlólista').click()
  const form = page.getByRole('form', { name: 'Új minimumszint' })
  await form.getByLabel('Hozzávaló').fill('tej')
  await form.getByLabel('Mennyiség').fill('1')
  await form.getByLabel('Mértékegység').selectOption('l')
  await form.getByRole('button', { name: 'Beállítom' }).click()
  await expect(page.getByRole('heading', { name: /^Javaslatok/ })).toBeVisible()

  const list = (await page.getByRole('region', { name: 'A listád' }).boundingBox())!
  const suggestions = (await page.getByRole('region', { name: /^Javaslatok/ }).boundingBox())!
  expect(suggestions.y).toBe(list.y)
  expect(suggestions.x).not.toBe(list.x)
})

test('the content is wider on a large screen and stays as it was at 1280 px', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 })
  await registerFreshHousehold(page)
  const wide = (await page.locator('main').boundingBox())!.width
  expect(wide).toBeGreaterThan(1300)
  expect(wide).toBeLessThanOrEqual(1400)

  await page.setViewportSize({ width: 1280, height: 900 })
  expect((await page.locator('main').boundingBox())!.width).toBeLessThanOrEqual(1184)
})

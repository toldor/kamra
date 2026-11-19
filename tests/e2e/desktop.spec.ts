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

  // The developer (2026-11-17): the sentence entry starts level with the item list, the summary above both (it shows
  // once there is an item).
  const card = page.locator('details', { has: page.locator('#new-item-heading') })
  await card.locator('summary').click()
  const form = page.getByRole('form', { name: 'Új tétel' })
  await form.getByLabel('Hozzávaló').fill('tej')
  await form.getByLabel('Mennyiség').fill('1')
  await form.getByLabel('Mértékegység').selectOption('l')
  await form.getByRole('button', { name: 'Hozzáadás' }).click()
  await expect(page.getByRole('group', { name: 'Összesítés' })).toBeVisible()
  await card.locator('summary').click()
  const quick = (await page.locator('details', { has: page.locator('#quick-heading') }).boundingBox())!
  const panel = (await page.locator('.panel', { has: page.locator('#pantry-heading') }).boundingBox())!
  expect(Math.abs(quick.y - panel.y)).toBeLessThanOrEqual(1)
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
  // Review V2: the last card keeps its bottom border.
  expect(await cards.last().evaluate((card) => getComputedStyle(card).borderBottomWidth)).toBe('1px')

  // Review V3: three columns on a wide screen too.
  await page.setViewportSize({ width: 1600, height: 900 })
  const wide = await Promise.all([0, 3].map(async (i) => (await cards.nth(i).boundingBox())!))
  expect(wide[1].y).toBeGreaterThan(wide[0].y)
})

// Review V1: the "Új tétel" card stays closed next to the sentence entry, also when the entry's switch answers late.
test('the new item card starts closed when the sentence entry answers late', async ({ page }) => {
  await page.route('**/api/v1/item-proposals/availability', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 1500))
    await route.continue()
  })
  await registerFreshHousehold(page)
  await expect(page.getByRole('heading', { name: 'Mondatos bevitel' })).toBeVisible()

  const open = await page.locator('details', { has: page.locator('#new-item-heading') }).evaluate((d) => (d as HTMLDetailsElement).open)
  expect(open).toBe(false)
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
  // The developer (2026-11-17): the list on the left, the suggestions on the right; the minimum levels as wide as both.
  expect(list.x).toBeLessThan(suggestions.x)
  const minimums = (await page.getByRole('region', { name: 'Minimumszintek' }).boundingBox())!
  expect(Math.round(minimums.x)).toBe(Math.round(list.x))
  expect(Math.round(minimums.x + minimums.width)).toBe(Math.round(suggestions.x + suggestions.width))
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

// The developer (2026-11-17): the Kérdezz box and its bubbles have rounded corners, unlike the rest of the app.
test('the chat box and its bubbles are rounded', async ({ page }) => {
  await registerFreshHousehold(page)
  await menu(page, 'Kérdezz').click()
  await page.getByLabel('Kérdésed').fill('Szia')
  await page.getByRole('button', { name: 'Küldés' }).click()
  await expect(page.getByRole('log').getByText('Szia! Miben segíthetek?')).toBeVisible()

  const radius = (selector: string) => page.locator(selector).first().evaluate((e) => parseFloat(getComputedStyle(e).borderTopLeftRadius))
  expect(await radius('.chat-page .panel')).toBeGreaterThanOrEqual(12)
  expect(await radius('.chat-message.user')).toBeGreaterThanOrEqual(12)
  expect(await radius('.chat-message.assistant')).toBeGreaterThanOrEqual(4)
  // Review W1: the small chat buttons keep the 44 px target of the project (ux_flows).
  const copy = (await page.getByRole('button', { name: 'Válasz másolása' }).boundingBox())!
  expect(copy.height).toBeGreaterThanOrEqual(44)
  expect(copy.width).toBeGreaterThanOrEqual(44)
})

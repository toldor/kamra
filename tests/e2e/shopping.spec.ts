import { expect, test, type Page } from '@playwright/test'

const password = 'correct horse battery staple'

async function expectNoHorizontalScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
  expect(overflow, 'horizontal overflow in px').toBeLessThanOrEqual(0)
}

// Every test registers its own account, so it starts with its own, empty household.
async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

// "Today" is the Budapest calendar day, like the backend's (Today.Of).
const inDays = (days: number) =>
  new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Budapest' }).format(new Date(Date.now() + days * 86_400_000))

async function addItem(page: Page, ingredient: string, amount: string, unit: string, daysToExpiry: number) {
  const form = page.getByRole('form', { name: 'Új tétel' })
  await form.getByLabel('Hozzávaló').fill(ingredient)
  await form.getByLabel('Mennyiség').fill(amount)
  await form.getByLabel('Mértékegység').selectOption(unit)
  await form.getByLabel('Lejárat (nem kötelező)').fill(inDays(daysToExpiry))
  await form.getByRole('button', { name: 'Hozzáadás' }).click()
  await expect(form.getByLabel('Hozzávaló')).toHaveValue('')
}

// P13 (ux_flows 3. flow, 5. levél): the recommendation, the cooking preview and the list show the same
// shortage; the tomatoes already on the list get no suggestion (M15), the used-up peppers do.
test('the shortage goes from the recipe to the list and the depleted item becomes a suggestion', async ({ page }) => {
  await registerFreshHousehold(page)
  await addItem(page, 'paprika', '500', 'g', 2)
  await addItem(page, 'paradicsom', '200', 'g', 1)
  await addItem(page, 'vöröshagyma', '300', 'g', 10)
  await addItem(page, 'étolaj', '1', 'l', 90)

  await page.getByRole('link', { name: 'Mit főzzek?' }).click()
  const card = page.getByRole('article', { name: 'Lecsó' })
  await expect(card).toContainText('paradicsom: 100 g hiányzik (200 g van, 300 g kell)')
  await card.getByRole('link', { name: 'Recept megtekintése' }).click()

  await expect(page.getByRole('group', { name: 'paradicsom' })).toContainText('Hiány: 100 g')
  await page.getByRole('button', { name: 'Hiány a bevásárlólistára' }).click()
  await expect(page.getByText('Felvettem a listára: paradicsom 100 g.')).toBeVisible()
  await expectNoHorizontalScroll(page)
  await page.getByRole('button', { name: 'Főzés megerősítése' }).click()

  await expect(page.getByText('Felvettem a bevásárlójavaslatok közé: paprika.')).toBeVisible()
  await page.getByRole('link', { name: 'Megnézem' }).click()

  await expect(page.getByRole('heading', { level: 1, name: 'Bevásárlólista' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Javaslatok (1)' })).toBeVisible()
  const suggestion = page.getByRole('article', { name: 'paprika' })
  await expect(suggestion).toContainText('főzés: Lecsó')
  await expect(page.getByRole('article', { name: 'paradicsom' })).toHaveCount(0)
  await expect(page.getByRole('checkbox', { name: 'paradicsom – 100 g' })).toBeVisible()
  await expectNoHorizontalScroll(page)

  await suggestion.getByLabel('Mennyit vegyél?').fill('500')
  await suggestion.getByRole('button', { name: 'Hozzáadom' }).click()
  await expect(page.getByText('Felvettem a listára: paprika 500 g.')).toBeVisible()
  await expect(page.getByRole('heading', { name: /Javaslatok/ })).toHaveCount(0)

  // The tick shows at once, before the save is done: wait for the save, or the reload below would cancel it.
  const saved = page.waitForResponse((r) => r.request().method() === 'PUT' && r.url().includes('/shopping-list/items/'))
  await page.getByRole('checkbox', { name: 'paradicsom – 100 g' }).check()
  expect((await saved).ok()).toBe(true)
  await expect(page.getByRole('checkbox', { name: 'paradicsom – 100 g' })).toBeChecked()
  await expect(page.getByRole('checkbox', { name: 'paprika – 500 g' })).not.toBeChecked()
  await expectNoHorizontalScroll(page)

  // The list is stored: after a reload it is the same.
  await page.reload()
  await expect(page.getByRole('checkbox', { name: 'paradicsom – 100 g' })).toBeChecked()
  await expect(page.getByRole('checkbox', { name: 'paprika – 500 g' })).toBeVisible()
})

// Regression (2026-10-31): a long ingredient name pushed the minimum row's delete button out of its panel.
test('a minimum level with a long name stays inside its panel', async ({ page }) => {
  await registerFreshHousehold(page)
  await page.getByRole('navigation', { name: 'Oldalak' }).getByRole('link', { name: 'Bevásárlólista' }).click()
  const form = page.getByRole('form', { name: 'Új minimumszint' })
  await form.getByLabel('Hozzávaló').fill('fagyasztott hasábburgonya')
  await form.getByLabel('Mennyiség').fill('200')
  await form.getByLabel('Mértékegység').selectOption('g')
  await form.getByRole('button', { name: 'Beállítom' }).click()

  const panel = await page.getByRole('region', { name: 'Minimumszintek' }).boundingBox()
  const button = await page.getByRole('button', { name: 'Minimumszint törlése: fagyasztott hasábburgonya' }).boundingBox()
  expect(button!.x + button!.width).toBeLessThanOrEqual(panel!.x + panel!.width)
  await expectNoHorizontalScroll(page)
})

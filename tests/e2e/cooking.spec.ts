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
  await expect(page.getByRole('listitem', { name: new RegExp(ingredient) }).first()).toBeVisible()
}

// P2: tomatoes in two items, the earlier expiring one is used first.
async function addLecsoStock(page: Page) {
  await addItem(page, 'paradicsom', '200', 'g', 1)
  await addItem(page, 'paradicsom', '400', 'g', 5)
  await addItem(page, 'paprika', '500', 'g', 2)
  await addItem(page, 'vöröshagyma', '300', 'g', 10)
  await addItem(page, 'étolaj', '1', 'l', 90)
}

async function openLecso(page: Page) {
  await page.getByRole('link', { name: 'Mit főzzek?' }).click()
  await page.getByRole('article', { name: 'Lecsó' }).getByRole('link', { name: 'Megfőztem' }).click()
  await expect(page.getByRole('heading', { name: 'Lecsó főzése' })).toBeVisible()
}

// US-4 main flow (ux_flows 2. flow) with the hand-calculated P2: 300 g of tomatoes, 200 g from the
// earlier item and 100 g from the later one, so 300 g stays.
test('cooks a suggested recipe and deducts the earliest expiring items first', async ({ page }) => {
  await registerFreshHousehold(page)
  await addLecsoStock(page)

  await openLecso(page)
  await expect(page.getByRole('group', { name: 'paradicsom' })).toContainText('– 200 g')
  await expect(page.getByRole('group', { name: 'paradicsom' })).toContainText('– 100 g')
  await expectNoHorizontalScroll(page)
  await page.getByRole('button', { name: 'Főzés megerősítése' }).click()

  await expect(page.getByText('Elkészült: Lecsó, 4 adag.')).toBeVisible()
  await expect(page.getByText('Elfogyott: paprika.')).toBeVisible()
  await expectNoHorizontalScroll(page)
  await page.getByRole('link', { name: 'Vissza a készlethez' }).click()
  const tomatoes = page.getByRole('listitem', { name: /paradicsom/ })
  await expect(tomatoes).toHaveCount(1)
  await expect(tomatoes).toContainText('300 g')
  await expect(page.getByRole('listitem', { name: /paprika/ })).toHaveCount(0)
})

// ADR-0008: a double click (or a repeated request) deducts only once.
test('a double click on the confirmation deducts once', async ({ page }) => {
  await registerFreshHousehold(page)
  await addLecsoStock(page)
  await openLecso(page)

  await page.getByRole('button', { name: 'Főzés megerősítése' }).dblclick()

  await expect(page.getByText('Elkészült: Lecsó, 4 adag.')).toBeVisible()
  await page.getByRole('link', { name: 'Vissza a készlethez' }).click()
  await expect(page.getByRole('listitem', { name: /paradicsom/ })).toContainText('300 g')
  await expect(page.getByRole('listitem', { name: /vöröshagyma/ })).toContainText('150 g')
})

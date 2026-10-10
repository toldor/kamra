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

async function addItem(page: Page, ingredient: string, amount: string, unit: string) {
  const form = page.getByRole('form', { name: 'Új tétel' })
  await form.getByLabel('Hozzávaló').fill(ingredient)
  await form.getByLabel('Mennyiség').fill(amount)
  await form.getByLabel('Mértékegység').selectOption(unit)
  await form.getByRole('button', { name: 'Hozzáadás' }).click()
}

// US-1: a pantry item without an expiry date gets an estimated one, marked as such.
test('adds an item without expiry and shows the estimated date', async ({ page }) => {
  await registerFreshHousehold(page)

  await addItem(page, 'tejföl', '20', 'dkg')

  const row = page.getByRole('listitem', { name: /tejföl/ })
  await expect(row).toContainText('20 dkg')
  await expect(row).toContainText('(becsült)')
  await expectNoHorizontalScroll(page)
})

// US-1: a decrease needs a reason, and deleting is a decrease to zero with a reason.
test('decreases with a reason and deletes an item', async ({ page }) => {
  await registerFreshHousehold(page)
  await addItem(page, 'tej', '1', 'l')
  const row = page.getByRole('listitem', { name: /tej\b/ })

  await row.getByRole('button', { name: 'Módosítás' }).click()
  const dialog = page.getByRole('dialog', { name: 'tej módosítása' })
  await dialog.getByLabel('Mennyiség').fill('0.6')
  await dialog.getByLabel('Miért csökken?').selectOption('consumed')
  await dialog.getByRole('button', { name: 'Mentés' }).click()
  await expect(row).toContainText('0,6 l')

  await row.getByRole('button', { name: 'Módosítás' }).click()
  await dialog.getByRole('button', { name: 'Törlés' }).click()
  await dialog.getByLabel('Miért csökken?').selectOption('discarded')
  await dialog.getByRole('button', { name: 'Törlés' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
  await expectNoHorizontalScroll(page)
})

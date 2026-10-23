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

// P17 and P18 on screen (T3 mini-spec, e2e): an own recipe with an own ingredient, the draft kept over a
// reload (M14), the recipe recommended from the stock, then archived and gone.
test('an own recipe with an own ingredient is recommended, and gone once archived', async ({ page }) => {
  await registerFreshHousehold(page)

  await page.getByRole('link', { name: 'Receptek' }).click()
  await page.getByRole('link', { name: 'Új recept' }).click()
  await page.getByLabel('Név').fill('Zabtejes kakaó')
  await page.getByLabel('Adagszám').fill('1')
  const row = page.getByRole('group', { name: '1. hozzávaló' })
  await row.getByLabel('Hozzávaló').fill('zabtej')
  await row.getByRole('button', { name: 'Új hozzávaló: zabtej' }).click()
  const create = row.getByRole('group', { name: 'Új hozzávaló: zabtej' })
  await create.getByLabel('Mivel méred?').selectOption('volume')
  await create.getByLabel('Kategória').selectOption('beverages')
  await create.getByRole('button', { name: 'Létrehozom' }).click()
  await expect(row.getByText('Létrehoztam: zabtej. Csak a te háztartásodban látszik.')).toBeVisible()
  await row.getByLabel('Mennyiség').fill('250')
  await row.getByLabel('Mértékegység').selectOption('ml')
  await expectNoHorizontalScroll(page)

  // M14: an interrupted form comes back after a reload.
  await page.reload()
  await expect(page.getByText('Visszatöltöttem a félbehagyott receptet.')).toBeVisible()
  await expect(page.getByLabel('Név')).toHaveValue('Zabtejes kakaó')
  await expect(page.getByRole('group', { name: '1. hozzávaló' }).getByLabel('Mennyiség')).toHaveValue('250')
  await page.getByRole('button', { name: 'Mentés' }).click()

  await expect(page.getByText('Elmentettem a receptet: Zabtejes kakaó.')).toBeVisible()
  await expect(page.getByRole('article', { name: 'Zabtejes kakaó' })).toContainText('Saját recept')
  await expectNoHorizontalScroll(page)

  await page.getByRole('link', { name: 'Készlet' }).click()
  const form = page.getByRole('form', { name: 'Új tétel' })
  await form.getByLabel('Hozzávaló').fill('zabtej')
  await form.getByLabel('Mennyiség').fill('1')
  await form.getByLabel('Mértékegység').selectOption('l')
  await form.getByLabel('Lejárat (nem kötelező)').fill(inDays(7))
  await form.getByRole('button', { name: 'Hozzáadás' }).click()
  await expect(form.getByLabel('Hozzávaló')).toHaveValue('')

  await page.getByRole('link', { name: 'Mit főzzek?' }).click()
  await expect(page.getByRole('article', { name: 'Zabtejes kakaó' })).toBeVisible()

  await page.getByRole('link', { name: 'Receptek' }).click()
  page.once('dialog', (dialog) => void dialog.accept())
  const archived = page.waitForResponse((r) => r.url().endsWith('/archive') && r.request().method() === 'POST')
  await page.getByRole('button', { name: 'Archiválás: Zabtejes kakaó' }).click()
  expect((await archived).status()).toBe(204)
  await expect(page.getByText('Archiváltam: Zabtejes kakaó.')).toBeVisible()
  await expect(page.getByRole('article', { name: 'Zabtejes kakaó' })).toHaveCount(0)

  await page.getByRole('link', { name: 'Mit főzzek?' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Mit főzzek?' })).toBeVisible()
  await expect(page.getByRole('article', { name: 'Zabtejes kakaó' })).toHaveCount(0)
})

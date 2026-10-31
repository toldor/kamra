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

// ux_flows a11y: after the dialog closes, the keyboard focus is back on the button that opened it.
test('returns the focus to the edit button after saving', async ({ page }) => {
  await registerFreshHousehold(page)
  await addItem(page, 'tej', '1', 'l')
  const edit = page.getByRole('listitem', { name: /tej\b/ }).getByRole('button', { name: /Módosítás/ })

  await edit.focus()
  await page.keyboard.press('Enter')
  const dialog = page.getByRole('dialog', { name: 'tej módosítása' })
  await dialog.getByRole('button', { name: 'Mentés' }).click()

  await expect(dialog).toBeHidden()
  await expect(edit).toBeFocused()
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

// ADR-0013: the self-hosted fonts cover ő and ű (latin-ext), and the layout follows the designs:
// on a desktop the new item form stands beside the list (C3), at 360 px above it (C2).
test('loads the fonts for ő and ű and lays the pantry out for the screen width', async ({ page }, testInfo) => {
  await registerFreshHousehold(page)
  await addItem(page, 'tejföl', '20', 'dkg')
  await expect(page.getByRole('listitem', { name: /tejföl/ })).toBeVisible()

  const faces = await page.evaluate(async () => {
    const load = async (font: string) =>
      (await document.fonts.load(font, 'őű')).map((f) => `${f.family} ${f.unicodeRange}`).join(' | ')
    return { slab: await load('700 16px "Zilla Slab"'), sans: await load('400 16px "IBM Plex Sans"') }
  })
  expect(faces.slab).toContain('U+100-2BA')
  expect(faces.sans).toContain('U+100-2BA')

  const form = await page.getByRole('form', { name: 'Új tétel' }).boundingBox()
  const list = await page.getByRole('list').filter({ has: page.getByRole('listitem', { name: /tejföl/ }) }).boundingBox()
  if (testInfo.project.name === 'mobile-360') expect(form!.y + form!.height).toBeLessThanOrEqual(list!.y)
  else expect(form!.x + form!.width).toBeLessThanOrEqual(list!.x)
  await expectNoHorizontalScroll(page)
})

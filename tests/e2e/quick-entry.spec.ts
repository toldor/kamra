import { expect, test, type Page } from '@playwright/test'

// US-2 end to end (us2_minispec K16): the real SPA, backend and Gemini adapter against the LLM stub of
// docker-compose.e2e.yml (tests/e2e/llm-stub.mjs) - never a real LLM call (AGENTS.md 6.).

const password = 'correct horse battery staple'

async function expectNoHorizontalScroll(page: Page) {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth)
  expect(overflow, 'horizontal overflow in px').toBeLessThanOrEqual(0)
}

async function registerFreshHousehold(page: Page) {
  await page.goto('/')
  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(`e2e-${crypto.randomUUID()}@example.com`)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
}

// ux_flows 1. flow: one sentence, editable proposals, nothing saved before the approval.
test('turns a sentence into proposals and approves them into the pantry', async ({ page }) => {
  await registerFreshHousehold(page)

  await page.getByLabel('Mit vettél?').fill('Vettem 2 liter tejet és egy üveg pesztót, a tej holnapig jó')
  await page.getByRole('button', { name: 'Feldolgozom' }).click()

  await expect(page.getByRole('heading', { name: 'Javaslatok (2)' })).toBeFocused()
  await expect(page.getByText('Még üres a kamrád.'), 'nothing is saved before the approval').toBeVisible()
  await page.getByRole('button', { name: 'Kihagyom: egy üveg pesztót' }).click()
  await expect(page.getByText('Kihagytam: egy üveg pesztót.')).toBeVisible()
  await page.getByRole('button', { name: 'Jóváhagyom (1)' }).click()

  await expect(page.getByText('Felvettem a kamrádba: tej 2 l.')).toBeVisible()
  await expect(page.getByRole('listitem', { name: /tej/ })).toContainText('2 l')
  await expect(page.getByRole('heading', { name: /Javaslatok/ })).toHaveCount(0)
  await expectNoHorizontalScroll(page)
})

// S-1 / QA-3: an answer outside the schema (the stub sends no JSON): no retry, the text stays (external review).
test('an invalid LLM answer keeps the sentence and asks to rephrase', async ({ page }) => {
  await registerFreshHousehold(page)

  await page.getByLabel('Mit vettél?').fill('HIBÁS: 2 liter tej')
  await page.getByRole('button', { name: 'Feldolgozom' }).click()

  await expect(page.getByRole('form', { name: 'Mondatos bevitel' }).getByRole('alert'))
    .toHaveText('Nem sikerült értelmezni a mondatot. A szöveged megmaradt: próbáld újra, fogalmazd át, vagy rögzítsd tételenként.')
  await expect(page.getByLabel('Mit vettél?')).toHaveValue('HIBÁS: 2 liter tej')
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
})

// S-1 / QA-3: the LLM is down (the stub answers 503 to the attempt and the retry); the text stays and the
// manual form is one click away.
test('an LLM outage keeps the sentence and offers the manual form', async ({ page }) => {
  await registerFreshHousehold(page)

  await page.getByLabel('Mit vettél?').fill('KIESÉS: 2 liter tej')
  await page.getByRole('button', { name: 'Feldolgozom' }).click()

  await expect(page.getByRole('form', { name: 'Mondatos bevitel' }).getByRole('alert'))
    .toHaveText('Most nem tudom feldolgozni a mondatot, mert a szövegfeldolgozó nem válaszol. A szöveged megmaradt: próbáld újra pár perc múlva, vagy rögzítsd tételenként.')
  await expect(page.getByLabel('Mit vettél?')).toHaveValue('KIESÉS: 2 liter tej')
  await page.getByRole('button', { name: 'Inkább tételenként rögzítem' }).click()
  await expect(page.getByRole('form', { name: 'Új tétel' }).getByLabel('Hozzávaló')).toBeFocused()
  await expectNoHorizontalScroll(page)
})

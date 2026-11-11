import { expect, test, type Page } from '@playwright/test'

// US-6 end to end (us6_minispec K13): the real SPA, backend, MCP tools and Gemini chat adapter against the LLM stub of
// docker-compose.e2e.yml (tests/e2e/llm-stub.mjs) - never a real LLM call (AGENTS.md 6.). The stub asks for the
// expiring items, checks that its thought signature came back, and answers from the real tool result.

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

async function openChat(page: Page) {
  await page.getByRole('link', { name: 'Kérdezz' }).click()
  await expect(page.getByRole('heading', { level: 1, name: 'Kérdezz a kamrádról' })).toBeVisible()
}

async function ask(page: Page, question: string) {
  await page.getByLabel('Kérdésed').fill(question)
  await page.getByRole('button', { name: 'Küldés' }).click()
}

// ux_flows „Kérdezz”: the answer comes from the household's own pantry, through the MCP tool.
test('answers what expires soon from the own pantry and names the tool it looked at', async ({ page }) => {
  await registerFreshHousehold(page)
  // The stub's sentence: 2 l milk that is good until tomorrow (soon expiring).
  await page.getByLabel('Mit vettél?').fill('Vettem 2 liter tejet, a tej holnapig jó')
  await page.getByRole('button', { name: 'Feldolgozom' }).click()
  await page.getByRole('button', { name: 'Kihagyom: egy üveg pesztót' }).click()
  await page.getByRole('button', { name: 'Jóváhagyom (1)' }).click()
  await expect(page.getByText('Felvettem a kamrádba: tej 2 l.')).toBeVisible()

  await openChat(page)
  await page.getByRole('button', { name: 'Mi jár le hamarosan?' }).click()

  const log = page.getByRole('log')
  await expect(log.getByText('Hamarosan lejár: tej.')).toBeVisible()
  await expect(log.getByText('Megnéztem: a hamarosan lejáró tételeket.')).toBeVisible()
  await expectNoHorizontalScroll(page)
})

// S-1 / QA-3 for the chat: an outage after the retry; the question stays, the rest of the app works.
test('an outage keeps the question and says so', async ({ page }) => {
  await registerFreshHousehold(page)
  await openChat(page)

  await ask(page, 'KIESÉS: mi jár le?')

  await expect(page.getByRole('alert')).toHaveText(
    'Most nem tudok válaszolni, mert a szövegfeldolgozó nem válaszol. A kérdésed megmaradt: próbáld újra pár perc múlva.')
  await expect(page.getByLabel('Kérdésed')).toHaveValue('KIESÉS: mi jár le?')
  await page.getByRole('link', { name: 'Készlet' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
})

// scope_contract US-6: the chat changes nothing; it says where the change can be made.
test('a request to change something is answered with the page where it can be done', async ({ page }) => {
  await registerFreshHousehold(page)
  await openChat(page)

  await ask(page, 'Töröld a tejet!')

  const log = page.getByRole('log')
  await expect(log.getByText('Itt nem tudom törölni a tételeket. Ezt a Készlet oldalon teheted meg.')).toBeVisible()
  await expect(log.getByText(/^Megnéztem:/)).toHaveCount(0)
})

// ADR-0010 4.: a call of a tool that does not exist (the stub asks for delete_pantry_items) ends the question.
test('a call of an unknown tool is refused and nothing changes', async ({ page }) => {
  await registerFreshHousehold(page)
  await openChat(page)

  await ask(page, 'HIBÁS: mi van?')

  await expect(page.getByRole('alert')).toHaveText(
    'Erre most nem sikerült válaszolnom. Kérdezd meg másképp, vagy nézd meg közvetlenül a Készlet, a Mit főzzek? vagy a Bevásárlólista oldalon.')
  await expect(page.getByLabel('Kérdésed')).toHaveValue('HIBÁS: mi van?')
})

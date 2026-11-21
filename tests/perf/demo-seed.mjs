// Seeds the demo account of the public instance (t1_minispec K4, deploy_runbook.md "Élő példány"). Node 24, built-in
// modules only. The state matches demo_script.md after its 2nd step, plus a makeable recipe, a minimum level and a
// shopping list item, with expiry dates relative to today:
//   BASE_URL=https://<name>.cloudapp.azure.com DEMO_PASSWORD=... [INVITE_CODE=...] [DEMO_EMAIL=tomi@pelda.hu] node tests/perf/demo-seed.mjs
// Exit code: 0 seeded and checked, 1 any error, 2 the account already exists (restore the database first).
// The password is never printed.
import { Browser } from './client.mjs'

const base = process.env.BASE_URL
const email = process.env.DEMO_EMAIL || 'tomi@pelda.hu'
const password = process.env.DEMO_PASSWORD
const inviteCode = process.env.INVITE_CODE

// Not process.exit(): on Windows it can abort while fetch still holds a connection; the exit code is set at the end.
class SeedError extends Error {
  constructor(message, code = 1) {
    super(message)
    this.code = code
  }
}

function fail(message, code = 1) {
  throw new SeedError(message, code)
}

// The app is Hungarian: "tomorrow" is the Budapest calendar day, not the UTC one.
const day = (offset) =>
  new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Budapest' }).format(new Date(Date.now() + offset * 86_400_000))

// null expiry: the app estimates it from the category (US-1).
const pantry = [
  ['paprika', 500, 'g', day(2)],
  ['paradicsom', 200, 'g', day(1)],
  ['vöröshagyma', 300, 'g', null],
  ['étolaj', 1, 'l', null],
  ['tojás', 6, 'db', day(12)],
  ['vaj', 100, 'g', day(20)],
  ['só', 1, 'kg', null],
  ['bors', 50, 'g', null],
  ['tej', 1, 'l', day(2)],
]

async function expectOk(response, what) {
  if (response.ok) return response
  const body = await response.text()
  let code = ''
  try {
    code = JSON.parse(body).code ?? ''
  } catch {
    // not a ProblemDetails body
  }
  fail(`${what}: HTTP ${response.status} ${code}`.trim())
}

async function main() {
  if (!base) fail('Add meg a BASE_URL-t, például: https://kamra-xyz.swedencentral.cloudapp.azure.com')
  if (!password || password.length < 15) fail('Add meg a DEMO_PASSWORD-öt (legalább 15 karakter).')

  const browser = new Browser(base)
  await browser.refreshToken()

  const registered = await browser.call('POST', '/api/v1/auth/register', { email, password, ...(inviteCode ? { inviteCode } : {}) })
  if (registered.status === 409) fail(`A fiók már létezik (${email}); a betöltéshez előbb állítsd vissza az adatbázist (deploy_runbook.md).`, 2)
  await expectOk(registered, 'Regisztráció')
  await browser.refreshToken()

  const ingredients = await (await expectOk(await browser.call('GET', '/api/v1/ingredients'), 'Hozzávalók')).json()
  const byName = (name) => ingredients.find((ingredient) => ingredient.name === name) ?? fail(`Nincs ilyen hozzávaló: ${name}`)

  for (const [name, amount, unit, expiryDate] of pantry) {
    await expectOk(await browser.call('POST', '/api/v1/pantry-items', { ingredientId: byName(name).id, amount, unit, expiryDate }), `Készlet: ${name}`)
  }
  await expectOk(await browser.call('PUT', `/api/v1/ingredients/${byName('tej').id}/minimum`, { amount: 1, unit: 'l' }), 'Minimumszint: tej')
  await expectOk(await browser.call('POST', '/api/v1/shopping-list/items', { ingredientId: byName('kenyér').id, amount: 1, unit: 'db' }), 'Bevásárlólista: kenyér')

  // The demo needs these two on "Mit főzzek?" (demo_script.md 4th step).
  const recommendations = await (await expectOk(await browser.call('GET', '/api/v1/recommendations'), 'Ajánlás')).json()
  const names = (list) => list.map((recommendation) => recommendation.name)
  if (!names(recommendations.makeable).includes('Rántotta')) fail('A Rántotta nem elkészíthető a betöltés után.')
  if (!names(recommendations.almostMakeable).includes('Lecsó')) fail('A Lecsó nem majdnem elkészíthető a betöltés után.')

  console.log(`Kész: ${email} – ${pantry.length} készlettétel, minimumszint (tej), bevásárlólista (kenyér); ` +
    `elkészíthető: ${names(recommendations.makeable).join(', ')}; majdnem: ${names(recommendations.almostMakeable).join(', ')}.`)
}

try {
  await main()
} catch (error) {
  console.error(error instanceof SeedError ? error.message : `Váratlan hiba: ${error.message}`)
  process.exitCode = error instanceof SeedError ? error.code : 1
}

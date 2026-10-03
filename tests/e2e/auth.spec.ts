import { expect, test } from '@playwright/test'

const password = 'correct horse battery staple'
const newEmail = () => `e2e-${crypto.randomUUID()}@example.com`

// Walking skeleton smoke test: the real browser accepts the Secure, SameSite=Strict session cookie,
// the antiforgery token round trip works, and the Api serves the SPA on the same origin (ADR-0006).
test('register, see the empty pantry, log out and log back in', async ({ page }) => {
  const email = newEmail()
  await page.goto('/')

  await page.getByRole('button', { name: /Regisztrálj/ }).click()
  await page.getByLabel('E-mail-cím').fill(email)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()

  await page.reload()
  await expect(page.getByText('Még üres a kamrád.'), 'the session survives a reload').toBeVisible()

  await page.getByRole('button', { name: 'Kijelentkezés' }).click()
  await expect(page.getByRole('heading', { name: 'Bejelentkezés' })).toBeVisible()

  await page.getByLabel('E-mail-cím').fill(email)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Bejelentkezés' }).click()
  await expect(page.getByText('Még üres a kamrád.')).toBeVisible()
})

// ux_flows H4: one message for a wrong e-mail or password, and the e-mail stays filled in.
test('a failed login shows the H4 message and keeps the e-mail', async ({ page }) => {
  const email = newEmail()
  await page.goto('/')

  await page.getByLabel('E-mail-cím').fill(email)
  await page.getByLabel('Jelszó').fill('a wrong but long password')
  await page.getByRole('button', { name: 'Bejelentkezés' }).click()

  await expect(page.getByRole('alert')).toHaveText('Hibás e-mail-cím vagy jelszó.')
  await expect(page.getByLabel('E-mail-cím')).toHaveValue(email)
})

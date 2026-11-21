// Load measurement of the "Mit főzzek?" endpoint (quality_attributes.md S-3, performance.md). Node 24, built-in
// modules only. Start a stack with a raised login limit first, e.g.
//   $env:API_PORT='8091'; $env:POSTGRES_PORT='5434'; $env:RateLimiting__Auth__PermitLimit='1000'
//   docker compose -p kamra-perf up -d --build
//   node tests/perf/recommendations.mjs http://localhost:8091
// Each virtual user registers a fresh household and gets ITEMS pantry items; then the users ask for recommendations
// in parallel, WARMUP requests first, then REQUESTS measured ones. Prints the latency percentiles as JSON.
import { Browser } from './client.mjs'

const base = process.argv[2] ?? 'http://localhost:8080'
const USERS = Number(process.env.USERS ?? 10)
const ITEMS = Number(process.env.ITEMS ?? 50)
const WARMUP = Number(process.env.WARMUP ?? 50)
const REQUESTS = Number(process.env.REQUESTS ?? 500)
const unitOf = { mass: 'g', volume: 'ml', count: 'db' }

// A deterministic pseudo-random sequence, so every run builds the same pantries.
let seed = 42
const random = () => ((seed = (seed * 1103515245 + 12345) % 2147483648) / 2147483648)

async function household(ingredients) {
  const browser = new Browser(base)
  await browser.refreshToken()
  const email = `perf-${crypto.randomUUID()}@example.com`
  const registered = await browser.call('POST', '/api/v1/auth/register', { email, password: 'correct horse battery staple' })
  if (!registered.ok) throw new Error(`register: ${registered.status}`)
  await browser.refreshToken()
  for (let i = 0; i < ITEMS; i++) {
    const ingredient = ingredients[Math.floor(random() * ingredients.length)]
    const expiry = new Date(Date.now() + Math.floor(random() * 30) * 86_400_000).toISOString().slice(0, 10)
    const amount = ingredient.dimension.toLowerCase() === 'count' ? 1 + Math.floor(random() * 10) : 100 + Math.floor(random() * 900)
    const added = await browser.call('POST', '/api/v1/pantry-items',
      { ingredientId: ingredient.id, amount, unit: unitOf[ingredient.dimension.toLowerCase()], expiryDate: expiry })
    if (!added.ok) throw new Error(`pantry item: ${added.status} ${await added.text()}`)
  }
  return browser
}

const percentile = (sorted, p) => sorted[Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1)]

const first = new Browser(base)
await first.refreshToken()
await first.call('POST', '/api/v1/auth/register', { email: `perf-${crypto.randomUUID()}@example.com`, password: 'correct horse battery staple' })
const ingredients = (await (await first.call('GET', '/api/v1/ingredients')).json())
const users = []
for (let u = 0; u < USERS; u++) users.push(await household(ingredients))

async function run(total) {
  const times = []
  let errors = 0
  let next = 0
  await Promise.all(users.map(async (browser) => {
    while (next < total) {
      next++
      const start = performance.now()
      const response = await browser.call('GET', '/api/v1/recommendations')
      await response.arrayBuffer()
      times.push(performance.now() - start)
      if (!response.ok) errors++
    }
  }))
  return { times: times.sort((a, b) => a - b), errors }
}

await run(WARMUP)
const { times, errors } = await run(REQUESTS)
const round = (ms) => Math.round(ms * 10) / 10
console.log(JSON.stringify({
  users: USERS, itemsPerHousehold: ITEMS, requests: times.length, errors,
  p50: round(percentile(times, 50)), p95: round(percentile(times, 95)), p99: round(percentile(times, 99)), max: round(times.at(-1)),
}))

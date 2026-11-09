// US-2 e2e (us2_minispec K16, plan D8): a stand-in for the Gemini generateContent endpoint, so the e2e stack runs the
// real adapter, backend and SPA without a real LLM call (AGENTS.md 6.). Built-in Node modules only.
// The answer depends on the user's sentence: "KIESÉS" -> 503 (an outage, also on the retry), "HIBÁS" -> an answer
// outside the schema, "SEMMI" -> no items; anything else -> two items, milk and an off-list pesto.
import { createServer } from 'node:http'

const port = Number(process.env.PORT ?? 8089)

const answer = (items) => ({
  candidates: [{ content: { parts: [{ text: JSON.stringify({ items }) }] }, finishReason: 'STOP' }],
  usageMetadata: { promptTokenCount: 500, candidatesTokenCount: 80 },
})

const milkAndPesto = [
  { text: '2 liter tejet', ingredient: 'tej', amount: 2, unit: 'liter', expiry: 'holnapig' },
  { text: 'egy üveg pesztót', ingredient: '-', amount: 1, unit: 'üveg', expiry: null },
]

function reply(response, status, body) {
  response.writeHead(status, { 'Content-Type': 'application/json' })
  response.end(JSON.stringify(body))
}

createServer((request, response) => {
  let raw = ''
  request.on('data', (chunk) => { raw += chunk })
  request.on('end', () => {
    if (request.method !== 'POST' || !request.url?.includes(':generateContent')) return reply(response, 404, { error: 'not found' })
    // The adapter must send its key in the header (ADR-0009), never in the URL.
    if (!request.headers['x-goog-api-key'] || request.url.includes('key=')) return reply(response, 403, { error: 'no key header' })

    let sentence = ''
    try {
      sentence = JSON.parse(raw).contents?.[0]?.parts?.[0]?.text ?? ''
    } catch {
      return reply(response, 400, { error: 'not JSON' })
    }

    if (sentence.includes('KIESÉS')) return reply(response, 503, { error: { code: 503, status: 'UNAVAILABLE' } })
    if (sentence.includes('HIBÁS')) {
      return reply(response, 200, { candidates: [{ content: { parts: [{ text: 'nem JSON' }] }, finishReason: 'STOP' }] })
    }
    if (sentence.includes('SEMMI')) return reply(response, 200, answer([]))
    return reply(response, 200, answer(milkAndPesto))
  })
}).listen(port, () => console.log(`LLM stub listening on ${port}`))

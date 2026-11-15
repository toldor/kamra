// US-2 e2e (us2_minispec K16, plan D8): a stand-in for the Gemini generateContent endpoint, so the e2e stack runs the
// real adapter, backend and SPA without a real LLM call (AGENTS.md 6.). Built-in Node modules only.
// The answer depends on the user's sentence: "KIESÉS" -> 503 (an outage, also on the retry), "HIBÁS" -> an answer
// outside the schema, "SEMMI" -> no items; anything else -> two items, milk and an off-list pesto.
// US-6 e2e (us6_minispec K13): a request with tools is a chat question. "lejár" or "jár le" -> a function call of get_expiring_items
// (with a thought signature that must come back unchanged, ADR-0010 4.), then an answer from the real tool result;
// "töröld" -> a refusal; "KIESÉS" -> 503; "HIBÁS" -> a call of a tool that does not exist; anything else -> a greeting.
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

const text = (value) => ({
  candidates: [{ content: { role: 'model', parts: [{ text: value }] }, finishReason: 'STOP' }],
  usageMetadata: { promptTokenCount: 900, candidatesTokenCount: 30 },
})

const call = (name) => ({
  candidates: [{
    content: { role: 'model', parts: [{ functionCall: { id: 'e2e-call', name, args: {} }, thoughtSignature: 'e2e-signature' }] },
    finishReason: 'STOP',
  }],
  usageMetadata: { promptTokenCount: 900, candidatesTokenCount: 10 },
})

// The chat: the first call answers the question, the second one (after the tool) answers from the tool result.
function chat(body) {
  const contents = body.contents ?? []
  const last = contents[contents.length - 1]
  const results = (last?.parts ?? []).filter((part) => part.functionResponse)
  if (results.length > 0) {
    const step = contents.find((content) => content.role === 'model' && content.parts?.some((part) => part.functionCall))
    if (step?.parts?.[0]?.thoughtSignature !== 'e2e-signature') return [400, { error: 'MISSING_THOUGHT_SIGNATURE' }]
    const names = (results[0].functionResponse.response.items ?? []).map((item) => item.name)
    return [200, text(names.length > 0 ? `Hamarosan lejár: ${names.join(', ')}.` : 'Most semmi nem jár le hamarosan.')]
  }

  const question = last?.parts?.[0]?.text ?? ''
  if (question.includes('KIESÉS')) return [503, { error: { code: 503, status: 'UNAVAILABLE' } }]
  if (question.includes('HIBÁS')) return [200, call('delete_pantry_items')]
  if (question.toLowerCase().includes('töröld')) return [200, text('Itt nem tudom törölni a tételeket. Ezt a Készlet oldalon teheted meg.')]
  if (/lejár|jár le/.test(question)) return [200, call('get_expiring_items')]
  return [200, text('Szia! Miben segíthetek?')]
}

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

    let body
    try {
      body = JSON.parse(raw)
    } catch {
      return reply(response, 400, { error: 'not JSON' })
    }

    if (body.tools) return reply(response, ...chat(body))
    const sentence = body.contents?.[0]?.parts?.[0]?.text ?? ''

    if (sentence.includes('KIESÉS')) return reply(response, 503, { error: { code: 503, status: 'UNAVAILABLE' } })
    if (sentence.includes('HIBÁS')) {
      return reply(response, 200, { candidates: [{ content: { parts: [{ text: 'nem JSON' }] }, finishReason: 'STOP' }] })
    }
    if (sentence.includes('SEMMI')) return reply(response, 200, answer([]))
    return reply(response, 200, answer(milkAndPesto))
  })
}).listen(port, () => console.log(`LLM stub listening on ${port}`))

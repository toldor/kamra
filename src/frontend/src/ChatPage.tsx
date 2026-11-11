import { useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { api, ApiError, errorMessage, type ChatMessage } from './api/client'

// US-6 "Kérdezz" (us6_minispec K11-K12 and its texts): a read-only chat about the own household. The conversation
// lives only here (ADR-0010 5.): the last 10 messages go with every question, a failed question is not kept.
// The answer is plain text (ADR-0006); the line under it names the tools the model looked at (K2).

const examples = ['Mi jár le hamarosan?', 'Mit főzhetek abból, ami otthon van?', 'Mi van a bevásárlólistán?']

const toolNames: Record<string, string> = {
  get_pantry_items: 'a készletet',
  get_expiring_items: 'a hamarosan lejáró tételeket',
  get_recommendations: 'az ajánlást',
  get_shopping_list: 'a bevásárlólistát',
}

// The chat's own texts for the codes it shares with the quick entry (us6_minispec, Hibák).
const chatErrors: Record<string, string> = {
  LLM_UNAVAILABLE: 'Most nem tudok válaszolni, mert a szövegfeldolgozó nem válaszol. A kérdésed megmaradt: próbáld újra pár perc múlva.',
  LLM_INVALID_RESPONSE: 'Erre most nem sikerült válaszolnom. Kérdezd meg másképp, vagy nézd meg közvetlenül a Készlet, a Mit főzzek? ' +
    'vagy a Bevásárlólista oldalon.',
  RATE_LIMITED: 'Most túl sok kérdést küldtél egymás után. Várj néhány percet.',
  LLM_DISABLED: 'A kérdezés most ki van kapcsolva.',
}

const questionMax = 500
const historyMax = 10
// ADR-0010 5.: an earlier message goes back with at most 2000 characters (an answer can be longer: review R1).
const historyTextMax = 2000

type Message = ChatMessage & { toolsUsed?: string[] }

// "Megnéztem: a készletet, az ajánlást és a bevásárlólistát." - or nothing when no tool was used.
function toolsLine(tools: string[] = []): string | null {
  // An unknown name is left out: the user never sees a technical name (review R5).
  const names = tools.filter((tool) => tool in toolNames).map((tool) => toolNames[tool])
  if (names.length === 0) return null
  const list = names.length === 1 ? names[0] : `${names.slice(0, -1).join(', ')} és ${names[names.length - 1]}`
  return `Megnéztem: ${list}.`
}

function Bubble({ message }: { message: Message }) {
  const line = toolsLine(message.toolsUsed)
  return (
    <div className={`chat-message ${message.role}`}>
      <span className="visually-hidden">{message.role === 'user' ? 'Te:' : 'Kamra:'}</span>
      <p className="chat-text">{message.text}</p>
      {line && <p className="hint">{line}</p>}
    </div>
  )
}

export function ChatPage({ onSignedOut }: { onSignedOut: (notice: string) => void }) {
  const [messages, setMessages] = useState<Message[]>([])
  const [pending, setPending] = useState<string | null>(null)
  const [question, setQuestion] = useState('')
  const [error, setError] = useState<string | null>(null)
  const field = useRef<HTMLTextAreaElement>(null)
  const busy = pending !== null

  async function send(text: string) {
    const trimmed = text.trim()
    if (busy) return
    if (!trimmed) {
      setError('Írd le a kérdésed.')
      field.current?.focus()
      return
    }

    setError(null)
    setPending(trimmed)
    const history = messages.slice(-historyMax).map(({ role, text: said }) => ({ role, text: said.slice(0, historyTextMax) }))
    try {
      const reply = await api.askChat(trimmed, history)
      setMessages((previous) => [...previous, { role: 'user', text: trimmed },
        { role: 'assistant', text: reply.answer, toolsUsed: reply.toolsUsed }])
      // What was typed while waiting stays (review R3).
      setQuestion((current) => (current.trim() === trimmed ? '' : current))
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut(caught.message)
        return
      }

      // The question stays to be sent again; it does not go into the history.
      setQuestion(trimmed)
      setError(caught instanceof ApiError ? chatErrors[caught.code] ?? caught.errors.question?.[0] ?? caught.message : errorMessage(caught))
    } finally {
      setPending(null)
    }

    field.current?.focus()
  }

  function submit(event: FormEvent) {
    event.preventDefault()
    void send(question)
  }

  // Enter sends, Shift+Enter starts a new line (K11).
  function keyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault()
      void send(question)
    }
  }

  function newConversation() {
    setMessages([])
    setError(null)
    setQuestion('')
    field.current?.focus()
  }

  return (
    <main className="chat-page">
      <div className="page-head">
        <h1 tabIndex={-1}>Kérdezz a kamrádról</h1>
        <button type="button" className="secondary" onClick={newConversation} disabled={busy || messages.length === 0}>
          Új beszélgetés
        </button>
      </div>
      <p>
        Kérdezd meg, mi van otthon, mi jár le hamarosan, mit főzhetsz, vagy mi van a bevásárlólistán. A válaszhoz csak a saját adataidat
        nézem meg, és semmit nem módosítok.
      </p>
      <p className="hint">A beszélgetést nem mentjük: ha elhagyod az oldalt, újrakezdődik.</p>

      <section className="panel" aria-label="Beszélgetés">
        <div role="log" aria-live="polite" className="panel-body chat-log">
          {/* One list with the waiting question last: it stays the same element when the answer comes, so a screen
              reader reads it out once (review R4). */}
          {[...messages, ...(pending !== null ? [{ role: 'user' as const, text: pending }] : [])]
            .map((message, index) => <Bubble key={index} message={message} />)}
        </div>
        {messages.length === 0 && !busy && (
          <div className="chat-examples">
            {examples.map((example) => (
              <button key={example} type="button" className="secondary" onClick={() => void send(example)}>{example}</button>
            ))}
          </div>
        )}
        <form className="chat-form" onSubmit={submit} noValidate>
          <label htmlFor="chat-question">Kérdésed</label>
          <textarea
            id="chat-question"
            ref={field}
            rows={3}
            maxLength={questionMax}
            value={question}
            onChange={(event) => setQuestion(event.target.value)}
            onKeyDown={keyDown}
            aria-invalid={error ? 'true' : undefined}
            aria-describedby="chat-error"
          />
          <div id="chat-error" role="alert" className="form-error">{error}</div>
          <button type="submit" disabled={busy}>{busy ? 'Gondolkodom…' : 'Küldés'}</button>
        </form>
      </section>
    </main>
  )
}

import { Fragment, useRef, useState, type Dispatch, type FormEvent, type KeyboardEvent, type SetStateAction } from 'react'
import { api, ApiError, errorMessage, type RecipeIdea } from './api/client'
import { newConversation, type Conversation, type Message } from './conversation'
import { formatAmount } from './format'
import { Icon, type IconName } from './Icon'
import { readDraft, storeRecipeIdea } from './recipeDraft'
import { routes } from './useHashRoute'

// US-6 "Kérdezz" (us6_minispec K11-K12 and its texts): a read-only chat about the own household. The server keeps
// nothing (ADR-0010 5.): the last 10 messages go with every question, a failed question is not kept. The conversation
// lives in the app's memory (App), so it stays while the user visits other pages; signing out or reloading the page
// starts a new one (l3_l5_minispec L5). The answer is plain text (ADR-0006); the line under it names the tools the
// model looked at (K2).

// ui_polish_minispec 22. (K3): the examples as cards with an icon; the last one asks for a recipe idea.
const examples: [string, IconName][] = [
  ['Mi jár le hamarosan?', 'pantry'],
  ['Mit főzhetek abból, ami otthon van?', 'cook'],
  ['Mi van a bevásárlólistán?', 'list'],
  ['Adj egy receptet abból, ami otthon van.', 'recipes'],
]

// The tools in the "Megnéztem" line, with their icon (ui_polish_minispec 24., K5).
const toolNames: Record<string, [string, IconName]> = {
  get_pantry_items: ['a készletet', 'pantry'],
  get_expiring_items: ['a hamarosan lejáró tételeket', 'clock'],
  get_recommendations: ['az ajánlást', 'cook'],
  get_shopping_list: ['a bevásárlólistát', 'list'],
}

// ui_polish_minispec 23. (K4): the question a follow-up button sends after the last answer.
function followUp(message: Message): [string, string] | null {
  if (message.recipe) return ['Másik ötlet', 'Adj egy másik receptötletet.']
  if (message.toolsUsed?.includes('get_expiring_items')) return ['Mit főzzek ezekből?', 'Mit főzhetek a hamarosan lejáró tételekből?']
  return null
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


// "Megnéztem: a készletet, az ajánlást és a bevásárlólistát." - or nothing when no tool was used. Since
// ui_polish_minispec 24. (K5) each tool name has its icon; the sentence itself is unchanged.
function ToolsLine({ tools = [] }: { tools?: string[] }) {
  // An unknown name is left out: the user never sees a technical name (review R5).
  const known = tools.filter((tool) => tool in toolNames)
  if (known.length === 0) return null
  const names = known.map((tool) => toolNames[tool][0])
  const list = names.length === 1 ? names[0] : `${names.slice(0, -1).join(', ')} és ${names[names.length - 1]}`
  // The sentence in one piece for a screen reader; the same with icons on screen.
  return (
    <p className="hint tools-line">
      <span className="visually-hidden">Megnéztem: {list}.</span>
      <span aria-hidden="true">
        Megnéztem:{' '}
        {known.map((tool, index) => (
          <Fragment key={tool}>
            {index > 0 && (index === known.length - 1 ? ' és ' : ', ')}
            <span className="tool"><Icon name={toolNames[tool][1]} size={14} />{toolNames[tool][0]}</span>
          </Fragment>
        ))}
        .
      </span>
    </p>
  )
}

// ui_polish_minispec 20. (K1): the Kamra jar (the tab icon) next to its answers; decorative.
function Jar({ size = 28 }: { size?: number }) {
  return (
    <svg viewBox="0 0 32 32" width={size} height={size} aria-hidden="true" focusable="false" className="chat-avatar">
      <rect x="1" y="1" width="30" height="30" rx="7" className="jar-ground" />
      <rect x="11" y="5.5" width="10" height="4" rx="1.2" className="jar-lid" />
      <rect x="9" y="10" width="14" height="16.5" rx="3.5" className="jar-body" />
      <rect x="12" y="16" width="8" height="2.5" rx="1" className="jar-ground" />
    </svg>
  )
}

// ui_polish_minispec 27. (K8): copies an answer; hidden where the browser gives no clipboard (an insecure page). The
// developer (2026-11-17): an icon only - the name "Válasz másolása" for a screen reader, "Másolás" as the tooltip; done,
// a tick and "Kimásolva" for a screen reader; refused, "Nem sikerült" on screen (review W2).
function CopyButton({ text }: { text: string }) {
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle')
  if (!navigator.clipboard) return null

  async function copy() {
    try {
      await navigator.clipboard.writeText(text)
      setState('copied')
    } catch {
      setState('failed')
    }
    setTimeout(() => setState('idle'), 2000)
  }

  return (
    <>
      <button type="button" className="secondary chat-action icon-only" aria-label="Válasz másolása" title="Másolás" onClick={() => void copy()}>
        <Icon name={state === 'copied' ? 'check' : 'copy'} size={18} />
      </button>
      {state === 'copied' && <span className="visually-hidden">Kimásolva</span>}
      {state === 'failed' && <span className="hint">Nem sikerült</span>}
    </>
  )
}

// receptotlet_minispec 4. and Q7 (A): the idea in short (name, servings, ingredients; the steps are on the form), and
// the button that fills the Új recept form with it. A draft is replaced only after the browser's own confirmation, as
// archiving asks; an earlier idea has its own question (review U3).
function RecipeIdeaCard({ idea }: { idea: RecipeIdea }) {
  function edit() {
    const current = readDraft()
    const question = current?.aiSuggested
      ? 'A korábban átvett receptötlet helyére ez kerül. Folytatod?'
      : 'A félbehagyott új recepted helyére a chat receptötlete kerül. Folytatod?'
    if (current && !window.confirm(question)) return
    storeRecipeIdea(idea)
    window.location.hash = routes.newRecipe
  }

  return (
    <div role="group" aria-label={`Receptötlet: ${idea.name}`} className="chat-recipe">
      <p className="chat-recipe-name">{idea.name} ({idea.servings} adag)</p>
      <ul>
        {idea.ingredients.map((ingredient, index) => <li key={index}>{ingredient.name} {formatAmount(ingredient.amount, ingredient.unit)}</li>)}
      </ul>
      <button type="button" className="secondary" onClick={edit}>Szerkesztés és mentés</button>
    </div>
  )
}

// next: the follow-up of the last answer (K4), not while a question is on its way.
function Bubble({ message, next, onAsk }: { message: Message; next?: [string, string] | null; onAsk?: (question: string) => void }) {
  if (message.role === 'user') {
    return (
      <div className="chat-message user">
        <span className="visually-hidden">Te:</span>
        <p className="chat-text">{message.text}</p>
      </div>
    )
  }

  return (
    <div className="chat-row">
      <Jar />
      <div className="chat-message assistant">
        <span className="visually-hidden">Kamra:</span>
        <p className="chat-text">{message.text}</p>
        <ToolsLine tools={message.toolsUsed} />
        {message.recipe && <RecipeIdeaCard idea={message.recipe} />}
        <div className="chat-actions">
          <CopyButton text={message.text} />
          {next && onAsk && <button type="button" className="secondary chat-action" onClick={() => onAsk(next[1])}>{next[0]}</button>}
        </div>
      </div>
    </div>
  )
}

export function ChatPage({ conversation, onChange, onSignedOut }: {
  conversation: Conversation
  onChange: Dispatch<SetStateAction<Conversation>>
  onSignedOut: (notice: string) => void
}) {
  const { messages, pending, question, error } = conversation
  const field = useRef<HTMLTextAreaElement>(null)
  const busy = pending !== null

  // Changes only the conversation with this id: a late answer does not touch a newer one.
  function update(id: number, change: (current: Conversation) => Partial<Conversation>) {
    onChange((current) => (current.id === id ? { ...current, ...change(current) } : current))
  }

  const setQuestion = (text: string) => update(conversation.id, () => ({ question: text }))

  // typed: false for an example or a follow-up button - its question does not replace a typed draft (review W6).
  async function send(text: string, typed = true) {
    const trimmed = text.trim()
    const { id } = conversation
    if (busy) return
    if (!trimmed) {
      update(id, () => ({ error: 'Írd le a kérdésed.' }))
      field.current?.focus()
      return
    }

    update(id, () => ({ error: null, pending: trimmed }))
    const history = messages.slice(-historyMax).map(({ role, text: said }) => ({ role, text: said.slice(0, historyTextMax) }))
    try {
      const reply = await api.askChat(trimmed, history)
      update(id, (current) => ({
        pending: null,
        messages: [...current.messages, { role: 'user', text: trimmed },
          { role: 'assistant', text: reply.answer, toolsUsed: reply.toolsUsed, recipe: reply.recipe }],
        // What was typed while waiting stays (review R3).
        question: current.question.trim() === trimmed ? '' : current.question,
      }))
    } catch (caught) {
      if (caught instanceof ApiError && caught.status === 401) {
        onSignedOut(caught.message)
        return
      }

      // The question stays to be sent again; it does not go into the history.
      update(id, (current) => ({
        pending: null,
        question: !typed && current.question.trim() !== '' ? current.question : trimmed,
        error: caught instanceof ApiError ? chatErrors[caught.code] ?? caught.errors.question?.[0] ?? caught.message : errorMessage(caught),
      }))
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

  function startOver() {
    onChange(newConversation())
    field.current?.focus()
  }

  return (
    <main className="chat-page">
      <div className="page-head">
        <h1 tabIndex={-1}>Kérdezz a kamrádról</h1>
        <button type="button" className="secondary" onClick={startOver} disabled={busy || messages.length === 0}>
          Új beszélgetés
        </button>
      </div>
      <p>
        Kérdezd meg, mi van otthon, mi jár le hamarosan, mit főzhetsz, vagy mi van a bevásárlólistán. A válaszhoz csak a saját adataidat
        nézem meg, és semmit nem módosítok.
      </p>
      <p className="hint">
        A beszélgetést nem mentjük el: másik oldalra lépve megmarad, de az oldal újratöltésekor vagy kijelentkezéskor újrakezdődik.
      </p>

      <section className="panel" aria-label="Beszélgetés">
        <div role="log" aria-live="polite" className="panel-body chat-log">
          {/* One list with the waiting question last: it stays the same element when the answer comes, so a screen
              reader reads it out once (review R4). */}
          {[...messages, ...(pending !== null ? [{ role: 'user' as const, text: pending }] : [])]
            .map((message, index) => (
              <Bubble
                key={index}
                message={message}
                next={!busy && index === messages.length - 1 ? followUp(message) : null}
                onAsk={(text) => {
                  // Review W5: the follow-up button goes away while the answer comes; the focus waits in the field.
                  field.current?.focus()
                  void send(text, false)
                }}
              />
            ))}
          {/* ui_polish_minispec 21. (K2): the dots while the answer is on its way; the button says "Gondolkodom…". */}
          {busy && (
            <div className="chat-row" aria-hidden="true">
              <Jar />
              <div className="chat-message assistant typing"><span /><span /><span /></div>
            </div>
          )}
        </div>
        {messages.length === 0 && !busy && (
          <div className="chat-examples">
            {/* ui_polish_minispec 25. (K6): a greeting above the examples of an empty conversation. */}
            <div className="chat-welcome">
              <Jar size={56} />
              <p>Szia! Miben segíthetek a kamrával kapcsolatban?</p>
            </div>
            {examples.map(([example, icon]) => (
              <button key={example} type="button" className="secondary chat-example" onClick={() => void send(example, false)}>
                <Icon name={icon} />
                {example}
              </button>
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
            aria-describedby="chat-error chat-count"
          />
          {/* ui_polish_minispec 26. (K7). */}
          <p id="chat-count" className="hint chat-count">{question.length} / {questionMax}</p>
          <div id="chat-error" role="alert" className="form-error">{error}</div>
          <button type="submit" disabled={busy}>{busy ? 'Gondolkodom…' : 'Küldés'}</button>
        </form>
      </section>
    </main>
  )
}

import { useEffect, useRef, useState, type FormEvent } from 'react'
import { api, ApiError, errorMessage, failWith, type CategoryInfo, type Ingredient, type ItemProposal, type Unit } from './api/client'
import { useFieldErrors } from './fieldErrors'
import { allUnits, findIngredient, formatAmount, formatOn, unitsOf } from './format'
import { IngredientPicker } from './IngredientPicker'

// us2_minispec K15. The other codes of the sentence have their texts in messages.ts; this one only here,
// because RATE_LIMITED of the sign-in has another text (the backend title).
const sentenceRateLimited = 'Most túl sok mondatot küldtél egymás után. Várj néhány percet, vagy rögzítsd tételenként.'

// K15: the card goes away after these, with a notice.
const goneCodes = ['ITEM_PROPOSAL_NOT_FOUND', 'ITEM_PROPOSAL_STATE_CONFLICT']

const textOrder = ['text'] as const

// US-2 (us2_minispec K13): the sentence panel in the side column. The text stays after every error.
export function QuickEntryForm({ onProposed, onManual, onSignedOut }: {
  onProposed: (proposals: ItemProposal[]) => void
  onManual: () => void
  onSignedOut: (notice: string) => void
}) {
  const [text, setText] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const fields = useFieldErrors('quick', textOrder)

  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    setError(null)
    fields.setErrors({})
    try {
      onProposed((await api.submitItemProposals(text)).items)
    } catch (caught) {
      if (caught instanceof ApiError && caught.code === 'VALIDATION_FAILED') {
        fields.setErrors(caught.errors)
      } else if (caught instanceof ApiError && caught.code === 'RATE_LIMITED') {
        setError(sentenceRateLimited)
      } else {
        failWith(onSignedOut, setError)(caught)
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <form aria-labelledby="quick-heading" className="panel" onSubmit={submit} noValidate>
      <h2 id="quick-heading">Mondatos bevitel</h2>
      <label htmlFor="quick-text">Mit vettél?</label>
      <textarea {...fields.props('text', 'quick-hint')} rows={3} value={text} onChange={(e) => setText(e.target.value)} />
      {fields.error('text') || (
        <p id="quick-hint" className="hint">
          Írd le egy mondatban, például: »2 liter tej, 10 tojás és fél kiló paradicsom, a tej péntekig jó«. Mentés előtt átnézheted.
        </p>
      )}
      <div role="alert" className="form-error">{error}</div>
      <button type="submit" disabled={busy}>{busy ? 'Feldolgozás…' : 'Feldolgozom'}</button>
      {busy && <p className="hint">Ez néhány másodpercig is tarthat.</p>}
      <button type="button" className="secondary" onClick={onManual}>Inkább tételenként rögzítem</button>
    </form>
  )
}

type Draft = { ingredientName: string; amount: string; unit: Unit | ''; expiryDate: string; expiryTouched: boolean }
type Errors = Record<string, string[]>

const draftOf = (p: ItemProposal): Draft => ({
  ingredientName: p.ingredientName ?? '',
  amount: p.amount === null ? '' : String(p.amount),
  unit: p.unit ?? '',
  expiryDate: p.expiryDate ?? '',
  expiryTouched: false,
})

const nameOf = (p: ItemProposal, d: Draft) => d.ingredientName.trim() || p.ingredientName || p.text

// Checked here, so an incomplete card is not sent (the backend checks again).
function missing(draft: Draft, ingredient: Ingredient | undefined): Errors {
  const errors: Errors = {}
  if (!ingredient) errors.ingredientId = ['Válassz hozzávalót a listából.']
  if (draft.amount.trim() === '') errors.amount = ['Add meg a mennyiséget.']
  if (draft.unit === '') errors.unit = ['Add meg a mértékegységet.']
  return errors
}

// US-2 (us2_minispec K1: A, K13-K15): the proposals as editable cards; one "Jóváhagyom (n)" sends them one by one,
// what went in disappears, a refused card stays with its field errors. Nothing is saved without the approval.
export function ProposalList({ proposals, ingredients, categories, onIngredientCreated, onApproved, onNotice, onSignedOut }: {
  proposals: ItemProposal[]
  ingredients: Ingredient[]
  categories: CategoryInfo[]
  onIngredientCreated: (ingredient: Ingredient) => void
  onApproved: () => void
  onNotice: (notice: string) => void
  onSignedOut: (notice: string) => void
}) {
  const [cards, setCards] = useState(() => proposals.map((p) => ({ proposal: p, draft: draftOf(p) })))
  const [errors, setErrors] = useState<Record<string, Errors>>({})
  const [cardErrors, setCardErrors] = useState<Record<string, string>>({})
  const [busy, setBusy] = useState(false)
  const heading = useRef<HTMLHeadingElement>(null)

  useEffect(() => heading.current?.focus(), [])

  const update = (id: string, change: Partial<Draft>) =>
    setCards((current) => current.map((c) => (c.proposal.id === id ? { ...c, draft: { ...c.draft, ...change } } : c)))
  const remove = (ids: string[]) => setCards((current) => current.filter((c) => !ids.includes(c.proposal.id)))

  async function approveAll(event: FormEvent) {
    event.preventDefault()
    setBusy(true)
    const added: string[] = []
    const gone: string[] = []
    const messages: string[] = []
    const nextErrors: Record<string, Errors> = {}
    const nextCardErrors: Record<string, string> = {}
    for (const { proposal, draft } of cards) {
      const ingredient = findIngredient(ingredients, draft.ingredientName)
      const incomplete = missing(draft, ingredient)
      if (Object.keys(incomplete).length > 0) {
        nextErrors[proposal.id] = incomplete
        continue
      }

      // A recognised or edited date is sent; an estimate is left to the backend, as in the manual form.
      const sendExpiry = draft.expiryDate !== '' && (draft.expiryTouched || !proposal.expiryEstimated)
      try {
        const item = await api.approveItemProposal(proposal.id, {
          ingredientId: ingredient!.id,
          amount: Number(draft.amount),
          unit: draft.unit as Unit,
          ...(sendExpiry ? { expiryDate: draft.expiryDate } : {}),
        })
        added.push(`${item.ingredientName} ${formatAmount(item.amount, item.unit)}`)
        gone.push(proposal.id)
      } catch (caught) {
        if (caught instanceof ApiError && caught.status === 401) {
          onSignedOut(caught.message)
          return
        }

        if (caught instanceof ApiError && goneCodes.includes(caught.code)) {
          gone.push(proposal.id)
          messages.push(caught.message)
        } else if (caught instanceof ApiError && caught.code === 'VALIDATION_FAILED') {
          nextErrors[proposal.id] = caught.errors
        } else {
          nextCardErrors[proposal.id] = errorMessage(caught)
        }
      }
    }

    remove(gone)
    setErrors(nextErrors)
    setCardErrors(nextCardErrors)
    setBusy(false)
    const failed = Object.keys(nextErrors).length + Object.keys(nextCardErrors).length
    if (added.length > 0 && failed === 0) {
      messages.unshift(`Felvettem a kamrádba: ${added.join(', ')}.`)
    } else if (added.length > 0) {
      messages.unshift(failed === 1
        ? `Felvettem ${added.length} tételt. Egynél javítás kell, lásd a kártyán.`
        : `Felvettem ${added.length} tételt. ${failed} tételnél javítás kell, lásd a kártyákon.`)
    }

    if (messages.length > 0) onNotice(messages.join(' '))
    if (added.length > 0) onApproved()
  }

  async function skip(proposal: ItemProposal, draft: Draft) {
    try {
      await api.rejectItemProposal(proposal.id)
      onNotice(`Kihagytam: ${nameOf(proposal, draft)}.`)
      remove([proposal.id])
    } catch (caught) {
      if (caught instanceof ApiError && goneCodes.includes(caught.code)) {
        onNotice(caught.message)
        remove([proposal.id])
      } else {
        failWith(onSignedOut, (message) => setCardErrors((current) => ({ ...current, [proposal.id]: message })))(caught)
      }
    }
  }

  if (cards.length === 0) return null

  // Every card focuses its first invalid field; only the first card that needs a fix may do so.
  const firstToFix = cards.find((c) => errors[c.proposal.id])?.proposal.id

  return (
    <form aria-labelledby="proposals-heading" className="panel" onSubmit={approveAll} noValidate>
      <h2 id="proposals-heading" ref={heading} tabIndex={-1}>Javaslatok ({cards.length})</h2>
      <p className="hint">Nézd át, javíts, ha kell, majd hagyd jóvá. Jóváhagyás nélkül semmi nem kerül a kamrádba.</p>
      {cards.map(({ proposal, draft }) => (
        <ProposalCard
          key={proposal.id}
          proposal={proposal}
          draft={draft}
          errors={errors[proposal.id]}
          focusOnError={proposal.id === firstToFix}
          error={cardErrors[proposal.id]}
          busy={busy}
          ingredients={ingredients}
          categories={categories}
          onChange={(change) => update(proposal.id, change)}
          onSkip={() => void skip(proposal, draft)}
          onIngredientCreated={onIngredientCreated}
          onSignedOut={onSignedOut}
        />
      ))}
      <button type="submit" disabled={busy}>{busy ? 'Feldolgozás…' : `Jóváhagyom (${cards.length})`}</button>
    </form>
  )
}

const cardOrder = ['ingredientId', 'amount', 'unit', 'expiryDate'] as const
const dimensionText = { mass: 'tömegben mérhető: g, dkg vagy kg', volume: 'térfogatban mérhető: ml, dl vagy l', count: 'darabban mérhető: db' }

function ProposalCard({ proposal, draft, errors, focusOnError, error, busy, ingredients, categories, onChange, onSkip, onIngredientCreated, onSignedOut }: {
  proposal: ItemProposal
  draft: Draft
  errors: Errors | undefined
  focusOnError: boolean
  error: string | undefined
  busy: boolean
  ingredients: Ingredient[]
  categories: CategoryInfo[]
  onChange: (change: Partial<Draft>) => void
  onSkip: () => void
  onIngredientCreated: (ingredient: Ingredient) => void
  onSignedOut: (notice: string) => void
}) {
  const prefix = `proposal-${proposal.id}`
  const fields = useFieldErrors(prefix, cardOrder, focusOnError)
  const { setErrors } = fields
  useEffect(() => setErrors(errors ?? {}), [errors, setErrors])

  const ingredient = findIngredient(ingredients, draft.ingredientName)
  const units = ingredient ? unitsOf[ingredient.dimension] : allUnits
  const name = nameOf(proposal, draft)
  // The hints are read with their field (ux_flows a11y) and go away once the field is fixed.
  const hint = (field: string, shown: unknown) => (shown ? `${prefix}-${field}-hint` : undefined)
  const notFound = hint('ingredientId', proposal.ingredientId === null && !ingredient)
  const noAmount = hint('amount', draft.amount.trim() === '')
  const noUnit = hint('unit', draft.unit === '')
  const mismatch = proposal.unitMismatch && ingredient && noUnit
  const unrecognised = hint('expiryDate', proposal.expiryText && !draft.expiryTouched)

  return (
    <fieldset className="proposal">
      <legend>»{proposal.text}«</legend>
      <IngredientPicker
        ingredients={ingredients}
        value={draft.ingredientName}
        onChange={(value) => onChange({ ingredientName: value })}
        field={fields.props('ingredientId', notFound)}
        error={fields.error('ingredientId')}
        create={{ categories, onCreated: onIngredientCreated, onSignedOut }}
      />
      {notFound && (
        <p id={notFound} className="hint">Nem találtam a hozzávalók között: »{proposal.text}«. Válassz egyet, vagy vegyél fel újat.</p>
      )}
      {proposal.inStock && (
        <p className="hint">
          Már van otthon: {proposal.ingredientName} {formatAmount(proposal.inStock.amount.value, proposal.inStock.amount.unit)},
          {proposal.inStock.itemCount > 1 && ' a legkorábbi'} {formatOn(proposal.inStock.earliestExpiry)} jár le.
        </p>
      )}

      <div className="row">
        <div className="field">
          <label htmlFor={`${prefix}-amount`}>Mennyiség</label>
          <input
            {...fields.props('amount', noAmount)}
            type="number"
            inputMode="decimal"
            min="0"
            step="any"
            value={draft.amount}
            onChange={(e) => onChange({ amount: e.target.value })}
          />
        </div>
        <div className="field">
          <label htmlFor={`${prefix}-unit`}>Mértékegység</label>
          <select {...fields.props('unit', noUnit)} value={draft.unit} onChange={(e) => onChange({ unit: e.target.value as Unit | '' })}>
            {(draft.unit === '' || !units.includes(draft.unit)) && <option value="">Válassz</option>}
            {units.map((u) => <option key={u} value={u}>{u}</option>)}
          </select>
        </div>
      </div>
      {fields.error('amount') || (noAmount && <p id={noAmount} className="hint">Add meg a mennyiséget.</p>)}
      {fields.error('unit') || (mismatch
        ? <p id={noUnit} className="hint">A(z) {ingredient.name} {dimensionText[ingredient.dimension]}. Add meg így a mennyiséget.</p>
        : noUnit && <p id={noUnit} className="hint">Add meg a mértékegységet.</p>)}

      <label htmlFor={`${prefix}-expiryDate`}>
        Lejárat{proposal.expiryEstimated && !draft.expiryTouched && <> <span className="badge">becsült</span></>}
      </label>
      <input
        {...fields.props('expiryDate', unrecognised)}
        type="date"
        value={draft.expiryDate}
        onChange={(e) => onChange({ expiryDate: e.target.value, expiryTouched: true })}
      />
      {fields.error('expiryDate') || (unrecognised && (
        <p id={unrecognised} className="hint">»{proposal.expiryText}«: ezt nem tudtam dátummá alakítani, ezért becsült lejáratot adtam. Javítsd, ha kell.</p>
      ))}

      {error && <p role="alert" className="form-error">{error}</p>}
      <button type="button" className="secondary" aria-label={`Kihagyom: ${name}`} onClick={onSkip} disabled={busy}>Kihagyom</button>
    </fieldset>
  )
}

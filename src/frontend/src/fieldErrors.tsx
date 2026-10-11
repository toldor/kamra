import { useCallback, useEffect, useRef, useState } from 'react'

type Field = HTMLInputElement | HTMLSelectElement

// ux_flows a11y, shared by the pantry form and editor (the AuthForm pattern): each field gets its
// label id, aria-invalid and aria-describedby, its error text under it, and the first invalid
// field receives the focus so a screen reader reads its label and error.
export function useFieldErrors(prefix: string, order: readonly string[]) {
  const [errors, setErrors] = useState<Record<string, string[]>>({})
  const fields = useRef<Record<string, Field | null>>({})

  useEffect(() => {
    const first = order.find((field) => errors[field])
    if (first) fields.current[first]?.focus()
  }, [errors, order])

  const text = (field: string) => errors[field]?.join(' ')
  const focus = useCallback((field: string) => fields.current[field]?.focus(), [])

  return {
    setErrors,
    focus,
    props: (field: string, hintId?: string) => ({
      id: `${prefix}-${field}`,
      ref: (element: Field | null) => {
        fields.current[field] = element
      },
      'aria-invalid': text(field) ? true : undefined,
      'aria-describedby': text(field) ? `${prefix}-${field}-error` : hintId,
    }),
    error: (field: string) => text(field) && <p id={`${prefix}-${field}-error`} className="field-error">{text(field)}</p>,
  }
}

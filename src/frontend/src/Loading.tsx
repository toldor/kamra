// ui_polish_minispec 9.: placeholder rows in the shape of a list while it loads; a screen reader still hears
// "Betöltés…".
export function LoadingRows() {
  return (
    <div role="status" className="loading">
      <span className="visually-hidden">Betöltés…</span>
      <div className="skeleton" aria-hidden="true">
        <span />
        <span />
        <span />
      </div>
    </div>
  )
}

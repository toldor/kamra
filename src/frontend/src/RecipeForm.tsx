// N10 (T3): a new own recipe. Red phase: not implemented yet.
export function RecipeForm({ onSignedOut }: { onSignedOut: (notice: string) => void }) {
  void onSignedOut
  return <main><h1 tabIndex={-1}>Új recept</h1></main>
}

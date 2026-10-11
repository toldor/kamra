// US-4 confirmation screen: red-phase stub (S5).
export function CookingPage({ recipeId, onSignedOut }: { recipeId: string; onSignedOut: (notice: string) => void }) {
  return (
    <main data-recipe={recipeId} data-signed-out={typeof onSignedOut}>
      <h1 tabIndex={-1}>Főzés</h1>
    </main>
  )
}

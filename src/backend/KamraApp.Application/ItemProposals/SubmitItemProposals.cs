using KamraApp.Application.Common;
using KamraApp.Application.Ingredients;
using KamraApp.Application.Pantry;

namespace KamraApp.Application.ItemProposals;

// US-2: the sentence becomes item proposals; nothing goes into the pantry yet. The LLM gets the sentence and the
// names of the ingredients the household can see, nothing else (scope_contract.md, Külső API). The date, the unit
// and the ingredient id are decided here (us2_minispec K6-K8).
public sealed class SubmitItemProposals(IIngredientParser parser, IItemProposalRepository proposals, IIngredientRepository ingredients,
    IPantryRepository pantry, ICurrentHousehold household, TimeProvider time)
{
    public Task<SubmitItemProposalsResponse> ExecuteAsync(SubmitItemProposalsRequest? request, CancellationToken cancellationToken)
    {
        _ = (parser, proposals, ingredients, pantry, household, time); // red stage (spec-first): the T1 implementation uses them
        throw new NotImplementedException();
    }

    public ItemProposalAvailability Availability() => throw new NotImplementedException();
}

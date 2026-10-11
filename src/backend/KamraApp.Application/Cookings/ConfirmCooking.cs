using KamraApp.Application.Common;
using KamraApp.Application.Pantry;
using KamraApp.Application.Recommendations;

namespace KamraApp.Application.Cookings;

// US-4 / ADR-0008: validate, answer a repeated request from the stored cooking, check the stock, deduct
// FEFO and save in one transaction; after any save failure look for the request id first.
public sealed class ConfirmCooking(IRecipeRepository recipes, IPantryRepository pantry, ICookingRepository cookings,
    ICurrentHousehold household, TimeProvider time)
{
    public Task<ConfirmCookingResult> ExecuteAsync(ConfirmCookingRequest? request, CancellationToken cancellationToken) =>
        throw new NotImplementedException(
            $"red phase {recipes.GetHashCode() + pantry.GetHashCode() + cookings.GetHashCode() + household.GetHashCode() + time.GetHashCode()}");
}

using KamraApp.Application.Common;

namespace KamraApp.Application.Recipes;

// Stable codes and safe Hungarian titles of a recipe (error_handling.md); cooking builds on recipes, so it uses these.
public static class RecipeErrors
{
    public static NotFoundException RecipeNotFound() => new("RECIPE_NOT_FOUND", "Ez a recept nem található.");

    // M8 / D-c: the preview and a new cooking of an archived recipe; a repeated request still gets its stored answer.
    public static ConflictException RecipeArchived() =>
        new("RECIPE_ARCHIVED", "Ezt a receptet archiváltad, ezért már nem főzhető.");
}

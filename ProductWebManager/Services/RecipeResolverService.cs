// Services/RecipeResolverService.cs

using Microsoft.EntityFrameworkCore;
using ProductWebManager.Data;
using ProductWebManager.Models;

namespace ProductWebManager.Services;

public class RecipeResolverService
{
    private readonly ProductManagerContext _db;

    public RecipeResolverService(ProductManagerContext db)
    {
        _db = db;
    }

    public async Task<Recipe?> FindRecipeAsync(string title)
    {
        title = title.Trim().ToLower();

        return await _db.Recipes
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.Unit)

            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.Category)

            .FirstOrDefaultAsync(x =>
                x.Title.ToLower() == title);
    }
}
// Services/RecipeResolverService.cs

using Microsoft.EntityFrameworkCore;
using ProductWebManager.Data;
using ProductWebManager.Models;

namespace ProductWebManager.Services;

public class RecipeResolverService
{
    private readonly IDbContextFactory<ProductManagerContext> _dbContextFactory;

    public RecipeResolverService(IDbContextFactory<ProductManagerContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<Recipe?> FindRecipeAsync(string title)
    {
        title = title.Trim().ToLower();

        await using var db = await _dbContextFactory.CreateDbContextAsync();

        return await db.Recipes
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.Unit)

            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.Category)

            .FirstOrDefaultAsync(x =>
                x.Title != null && x.Title.ToLower() == title);
    }
}
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Models;

namespace ProductWebManager.Data
{
    public class ProductManagerContext: DbContext
    {
        public DbSet<Category> Categories {  get; set; }
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductAllergy> ProductAllergies { get; set; }
        public DbSet<FridgeItem> FridgeItems { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAllergy> UserAllergies { get; set; }
        public DbSet<Unit> Units { get; set; }
        public DbSet<RecipeIngredient> RecipeIngredients { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<FavoriteRecipe> FavoriteRecipes { get; set; }
        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanItem> MealPlanItems { get; set; }
        public DbSet<ShoppingList> ShoppingList { get; set; }
        public DbSet<ShoppingListItem> ShoppingListItems { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public ProductManagerContext(DbContextOptions<ProductManagerContext> options)
            :base(options)
        {
            
        }
    }
}

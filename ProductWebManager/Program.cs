using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Classes.AI;
using ProductWebManager.Components;
using ProductWebManager.Data;
using ProductWebManager.Models;
using ProductWebManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContextFactory<ProductManagerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<GigaChatHelper>();
builder.Services.Configure<GigaChatOptions>(
    builder.Configuration.GetSection("GigaChat"));

builder.Services.AddHttpClient<GigaChatHelper>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback =
            (sender, cert, chain, sslPolicyErrors) => true
    };
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RecipeResolverService>();
builder.Services.AddScoped<MealPlanBalancerService>();

builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<AuthService>());

builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ProductManagerContext>();

    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {

        var admin = new User
        {
            Login = "admin",
            Password = "admin",
            Profile = new UserProfile
            {
                Age = 22,
                Height = 182,
                Weight = 82,
                Gender = Gender.Male,
                ActivityLevel = ActivityLevel.Medium,
                Goal = GoalType.Maintain,
                TargetCalories = 2600
            }
        };

        var user = new User
        {
            Login = "user",
            Password = "user",
            Profile = new UserProfile
            {
                Age = 27,
                Height = 168,
                Weight = 61,
                Gender = Gender.Female,
                ActivityLevel = ActivityLevel.Low,
                Goal = GoalType.LoseWeight,
                TargetCalories = 1700
            }
        };

        db.Users.AddRange(admin, user);
        db.SaveChanges();

        // =========================================
        // ALLERGIES
        // =========================================

        var allergies = new List<Allergy>
        {
            new() { Name = "Лактоза" },
            new() { Name = "Глютен" },
            new() { Name = "Орехи" },
            new() { Name = "Яйца" },
            new() { Name = "Морепродукты" }
        };

        db.Allergies.AddRange(allergies);
        db.SaveChanges();

        // =========================================
        // USER ALLERGIES
        // =========================================

        db.UserAllergies.AddRange(
            new UserAllergy
            {
                UserId = admin.Id,
                AllergyId = allergies[0].Id
            },
            new UserAllergy
            {
                UserId = admin.Id,
                AllergyId = allergies[2].Id
            },
            new UserAllergy
            {
                UserId = user.Id,
                AllergyId = allergies[1].Id
            }
        );

        db.SaveChanges();

        // =========================================
        // UNITS
        // =========================================

        var units = new List<Unit>
        {
            new() { Name = "шт." },
            new() { Name = "г" },
            new() { Name = "кг" },
            new() { Name = "мл" },
            new() { Name = "л" }
        };

        db.Units.AddRange(units);
        db.SaveChanges();

        // =========================================
        // CATEGORIES
        // =========================================

        var categories = new List<Category>
        {
            new() { Name = "Мясо" },
            new() { Name = "Овощи" },
            new() { Name = "Молочные" },
            new() { Name = "Крупы" },
            new() { Name = "Фрукты" }
        };

        db.Categories.AddRange(categories);
        db.SaveChanges();

        // =========================================
        // PRODUCTS
        // =========================================

        var chicken = new Product
        {
            Name = "Курица",
            UnitId = units[1].Id,
            CategoryId = categories[0].Id,
            Price = 350,
            Proteins = 23,
            Fats = 8,
            Carbohydrates = 0,
            Calories = 180
        };

        var rice = new Product
        {
            Name = "Рис",
            UnitId = units[1].Id,
            CategoryId = categories[3].Id,
            Price = 120,
            Proteins = 7,
            Fats = 1,
            Carbohydrates = 74,
            Calories = 330
        };

        var milk = new Product
        {
            Name = "Молоко",
            UnitId = units[4].Id,
            CategoryId = categories[2].Id,
            Price = 90,
            Proteins = 3.2,
            Fats = 3.6,
            Carbohydrates = 4.7,
            Calories = 64
        };

        var tomato = new Product
        {
            Name = "Помидоры",
            UnitId = units[1].Id,
            CategoryId = categories[1].Id,
            Price = 150,
            Proteins = 1,
            Fats = 0.2,
            Carbohydrates = 4,
            Calories = 22
        };

        db.Products.AddRange(
            chicken,
            rice,
            milk,
            tomato
        );

        db.SaveChanges();

        // =========================================
        // FRIDGE
        // =========================================

        db.FridgeItems.AddRange(
            new FridgeItem
            {
                UserId = admin.Id,
                ProductId = chicken.Id,
                Quantity = 800,
                AddedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(3)
            },

            new FridgeItem
            {
                UserId = admin.Id,
                ProductId = milk.Id,
                Quantity = 1,
                AddedAt = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddDays(5)
            }
        );

        db.SaveChanges();

        // =========================================
        // RECIPES
        // =========================================

        var recipe1 = new Recipe
        {
            Title = "Курица с рисом",
            Description = "Простой белковый обед",
            Instructions =
                "1. Отварить рис\n" +
                "2. Обжарить курицу\n" +
                "3. Подать вместе",
            PrepTime = 10,
            CookTime = 25
        };

        var recipe2 = new Recipe
        {
            Title = "Овощной салат",
            Description = "Лёгкий салат",
            Instructions =
                "1. Нарезать овощи\n" +
                "2. Перемешать",
            PrepTime = 10,
            CookTime = 0
        };

        db.Recipes.AddRange(recipe1, recipe2);
        db.SaveChanges();

        // =========================================
        // RECIPE INGREDIENTS
        // =========================================

        db.RecipeIngredients.AddRange(
            new RecipeIngredient
            {
                RecipeId = recipe1.Id,
                ProductId = chicken.Id,
                Quantity = 300
            },

            new RecipeIngredient
            {
                RecipeId = recipe1.Id,
                ProductId = rice.Id,
                Quantity = 100
            },

            new RecipeIngredient
            {
                RecipeId = recipe2.Id,
                ProductId = tomato.Id,
                Quantity = 200
            }
        );

        db.SaveChanges();

        // =========================================
        // FAVORITES
        // =========================================

        db.FavoriteRecipes.Add(
            new FavoriteRecipe
            {
                UserId = admin.Id,
                RecipeId = recipe1.Id
            });

        db.SaveChanges();

        // =========================================
        // SHOPPING LIST
        // =========================================

        var shoppingList = new ShoppingList
        {
            UserId = admin.Id,
            Name = "Покупки на неделю",
            IsCompleted = false
        };

        db.ShoppingList.Add(shoppingList);
        db.SaveChanges();

        db.ShoppingListItems.AddRange(
            new ShoppingListItem
            {
                ShoppingListId = shoppingList.Id,
                ProductId = rice.Id,
                Quantity = 2,
                IsPurchased = false
            },

            new ShoppingListItem
            {
                ShoppingListId = shoppingList.Id,
                ProductId = tomato.Id,
                Quantity = 5,
                IsPurchased = false
            }
        );

        db.SaveChanges();

        // =========================================
        // MEAL PLAN
        // =========================================

        var mealPlan = new MealPlan
        {
            UserId = admin.Id,
            Name = "План на неделю"
        };

        db.MealPlans.Add(mealPlan);
        db.SaveChanges();

        db.MealPlanItems.AddRange(
            new MealPlanItem
            {
                MealPlanId = mealPlan.Id,
                RecipeId = recipe1.Id,
                Date = DateTime.Today,
                MealType = MealType.Lunch
            },

            new MealPlanItem
            {
                MealPlanId = mealPlan.Id,
                RecipeId = recipe2.Id,
                Date = DateTime.Today,
                MealType = MealType.Dinner
            }
        );

        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Components;
using ProductWebManager.Data;
using ProductWebManager.Models;
using ProductWebManager.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ProductManagerContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AuthService>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductManagerContext>();

    db.Database.EnsureCreated();

    if (!db.Users.Any())
    {
        var admin = new User
        {
            Login = "admin",
            Password = "admin",
            UserAllergies = new List<UserAllergie>()
        };
        var user = new User
        {
            Login = "user",
            Password = "user",
            UserAllergies = new List<UserAllergie>()
        };
        db.Users.AddRange(admin, user);
        db.SaveChanges();

        var allergies = new List<Allergie>
        {
            new() { Name = "Лактоза" },
            new() { Name = "Глютен" },
            new() { Name = "Орехи" },
            new() { Name = "Яйца" },
            new() { Name = "Мёд" },
            new() { Name = "Морепродукты" }
        };
        db.Allergies.AddRange(allergies);
        db.SaveChanges();

        var units = new List<Unit>
        {
            new() { Name = "шт." },
            new() { Name = "кг" },
            new() { Name = "г" },
            new() { Name = "л" },
            new() { Name = "мл" },
            new() { Name = "упак." },
            new() { Name = "ст.л." },
            new() { Name = "ч.л." },
            new() { Name = "зуб." },
            new() { Name = "щеп." }
        };
        db.Units.AddRange(units);
        db.SaveChanges();

        var categories = new List<Category>
        {
            new() { Name = "Молочные" },
            new() { Name = "Мясо" },
            new() { Name = "Овощи" },
            new() { Name = "Фрукты" },
            new() { Name = "Бакалея" },
            new() { Name = "Напитки" },
            new() { Name = "Заморозка" },
            new() { Name = "Другое" }
        };
        db.Categories.AddRange(categories);
        db.SaveChanges();

        var products = new List<Product>
{
    // Молочные
    new()
    {
        Name = "Молоко",
        UnitId = units[3].Id,
        CategoryId = categories[0].Id,
        Price = 80,
        Proteins = 3.2,
        Fats = 3.6,
        Carbohydrates = 4.8,
        Calories = 64
    },

    new()
    {
        Name = "Сыр",
        UnitId = units[2].Id,
        CategoryId = categories[0].Id,
        Price = 400,
        Proteins = 24,
        Fats = 30,
        Carbohydrates = 0,
        Calories = 360
    },

    new()
    {
        Name = "Яйца",
        UnitId = units[0].Id,
        CategoryId = categories[0].Id,
        Price = 120,
        Proteins = 12.7,
        Fats = 10.9,
        Carbohydrates = 0.7,
        Calories = 157
    },

    new()
    {
        Name = "Сливки",
        UnitId = units[4].Id,
        CategoryId = categories[0].Id,
        Price = 150,
        Proteins = 2.5,
        Fats = 20,
        Carbohydrates = 3.4,
        Calories = 206
    },

    new()
    {
        Name = "Сметана",
        UnitId = units[2].Id,
        CategoryId = categories[0].Id,
        Price = 90,
        Proteins = 2.8,
        Fats = 20,
        Carbohydrates = 3.2,
        Calories = 206
    },

    // Мясо
    new()
    {
        Name = "Курица",
        UnitId = units[2].Id,
        CategoryId = categories[1].Id,
        Price = 300,
        Proteins = 23,
        Fats = 9,
        Carbohydrates = 0,
        Calories = 190
    },

    new()
    {
        Name = "Бекон",
        UnitId = units[2].Id,
        CategoryId = categories[1].Id,
        Price = 450,
        Proteins = 12,
        Fats = 45,
        Carbohydrates = 1,
        Calories = 458
    },

    new()
    {
        Name = "Говядина",
        UnitId = units[2].Id,
        CategoryId = categories[1].Id,
        Price = 600,
        Proteins = 26,
        Fats = 15,
        Carbohydrates = 0,
        Calories = 250
    },

    // Овощи
    new()
    {
        Name = "Помидоры",
        UnitId = units[2].Id,
        CategoryId = categories[2].Id,
        Price = 150,
        Proteins = 1.1,
        Fats = 0.2,
        Carbohydrates = 3.7,
        Calories = 20
    },

    new()
    {
        Name = "Огурцы",
        UnitId = units[2].Id,
        CategoryId = categories[2].Id,
        Price = 100,
        Proteins = 0.8,
        Fats = 0.1,
        Carbohydrates = 2.8,
        Calories = 15
    },

    new()
    {
        Name = "Чеснок",
        UnitId = units[8].Id,
        CategoryId = categories[2].Id,
        Price = 30,
        Proteins = 6.5,
        Fats = 0.5,
        Carbohydrates = 29.9,
        Calories = 143
    },

    new()
    {
        Name = "Лук",
        UnitId = units[2].Id,
        CategoryId = categories[2].Id,
        Price = 40,
        Proteins = 1.4,
        Fats = 0,
        Carbohydrates = 10.4,
        Calories = 47
    },

    new()
    {
        Name = "Картофель",
        UnitId = units[1].Id,
        CategoryId = categories[2].Id,
        Price = 50,
        Proteins = 2,
        Fats = 0.4,
        Carbohydrates = 16.3,
        Calories = 77
    },

    new()
    {
        Name = "Морковь",
        UnitId = units[2].Id,
        CategoryId = categories[2].Id,
        Price = 60,
        Proteins = 1.3,
        Fats = 0.1,
        Carbohydrates = 6.9,
        Calories = 32
    },

    new()
    {
        Name = "Болгарский перец",
        UnitId = units[0].Id,
        CategoryId = categories[2].Id,
        Price = 120,
        Proteins = 1.3,
        Fats = 0.1,
        Carbohydrates = 5.3,
        Calories = 27
    },

    // Фрукты
    new()
    {
        Name = "Яблоки",
        UnitId = units[0].Id,
        CategoryId = categories[3].Id,
        Price = 90,
        Proteins = 0.4,
        Fats = 0.4,
        Carbohydrates = 9.8,
        Calories = 47
    },

    new()
    {
        Name = "Лимоны",
        UnitId = units[0].Id,
        CategoryId = categories[3].Id,
        Price = 60,
        Proteins = 0.9,
        Fats = 0.1,
        Carbohydrates = 3,
        Calories = 16
    },

    // Бакалея
    new()
    {
        Name = "Спагетти",
        UnitId = units[2].Id,
        CategoryId = categories[4].Id,
        Price = 80,
        Proteins = 11,
        Fats = 1.3,
        Carbohydrates = 70,
        Calories = 344
    },

    new()
    {
        Name = "Рис",
        UnitId = units[2].Id,
        CategoryId = categories[4].Id,
        Price = 100,
        Proteins = 7,
        Fats = 0.6,
        Carbohydrates = 74,
        Calories = 330
    },

    new()
    {
        Name = "Мука",
        UnitId = units[2].Id,
        CategoryId = categories[4].Id,
        Price = 60,
        Proteins = 10.3,
        Fats = 1.1,
        Carbohydrates = 70.6,
        Calories = 334
    },

    new()
    {
        Name = "Сахар",
        UnitId = units[2].Id,
        CategoryId = categories[4].Id,
        Price = 70,
        Proteins = 0,
        Fats = 0,
        Carbohydrates = 99.8,
        Calories = 399
    },

    new()
    {
        Name = "Соль",
        UnitId = units[9].Id,
        CategoryId = categories[4].Id,
        Price = 20,
        Proteins = 0,
        Fats = 0,
        Carbohydrates = 0,
        Calories = 0
    },

    new()
    {
        Name = "Оливковое масло",
        UnitId = units[4].Id,
        CategoryId = categories[4].Id,
        Price = 350,
        Proteins = 0,
        Fats = 99.8,
        Carbohydrates = 0,
        Calories = 898
    },

    new()
    {
        Name = "Овсянка",
        UnitId = units[2].Id,
        CategoryId = categories[4].Id,
        Price = 120,
        Proteins = 11.9,
        Fats = 5.8,
        Carbohydrates = 65.4,
        Calories = 352
    },

    // Напитки
    new()
    {
        Name = "Чай",
        UnitId = units[5].Id,
        CategoryId = categories[5].Id,
        Price = 150,
        Proteins = 20,
        Fats = 5.1,
        Carbohydrates = 6.9,
        Calories = 140
    },

    new()
    {
        Name = "Кофе",
        UnitId = units[5].Id,
        CategoryId = categories[5].Id,
        Price = 400,
        Proteins = 13.9,
        Fats = 14.4,
        Carbohydrates = 4.1,
        Calories = 201
    }
};
        db.Products.AddRange(products);
        db.SaveChanges();

        // === ПРОДУКТЫ В ХОЛОДИЛЬНИКЕ ===
        var fridgeItems = new List<FridgeItem>
        {
            new() { UserId = admin.Id, ProductId  = 1, Quanity = 1, ExpirationDate = DateTime.UtcNow.AddDays(5), AddetAt = DateTime.UtcNow.AddDays(-5) },
            new() { UserId = admin.Id, ProductId  = 2, Quanity = 200, ExpirationDate = DateTime.UtcNow.AddDays(2), AddetAt = DateTime.UtcNow.AddDays(-10) },
            new() { UserId = admin.Id, ProductId  = 3, Quanity = 10, ExpirationDate = DateTime.UtcNow.AddDays(14), AddetAt = DateTime.UtcNow.AddDays(-3) },
            new() { UserId = admin.Id, ProductId  = 6, Quanity = 500, ExpirationDate = DateTime.UtcNow, AddetAt = DateTime.UtcNow.AddDays(-2) },
            new() { UserId = admin.Id, ProductId  = 9, Quanity = 3, ExpirationDate = DateTime.UtcNow.AddDays(4), AddetAt = DateTime.UtcNow.AddDays(-1) },
            new() { UserId = admin.Id, ProductId  = 11, Quanity = 1, ExpirationDate = DateTime.UtcNow.AddDays(30), AddetAt = DateTime.UtcNow.AddDays(-7) },
            new() { UserId = admin.Id, ProductId  = 18, Quanity = 500, ExpirationDate = DateTime.UtcNow.AddDays(60), AddetAt = DateTime.UtcNow.AddDays(-15) },
            new() { UserId = admin.Id, ProductId  = 23, Quanity = 250, ExpirationDate = DateTime.UtcNow.AddDays(90), AddetAt = DateTime.UtcNow.AddDays(-20) },
            new() { UserId = admin.Id, ProductId  = 14, Quanity = 2, ExpirationDate = DateTime.UtcNow.AddDays(3), AddetAt = DateTime.UtcNow.AddDays(-4) },
            new() { UserId = admin.Id, ProductId  = 16, Quanity = 5, ExpirationDate = DateTime.UtcNow.AddDays(7), AddetAt = DateTime.UtcNow.AddDays(-2) },
        };
        db.FridgeItems.AddRange(fridgeItems);
        db.SaveChanges();

        // === РЕЦЕПТЫ ===
        var recipes = new List<Recipe>
        {
            new()
            {
                Title = "Паста Карбонара",
                Description = "Классическая итальянская паста с беконом, яйцами и пармезаном. Сливочный соус обволакивает каждую макаронину.",
                Instructions = "1. Отварить спагетти до al dente.\n2. Обжарить бекон до хрустящей корочки.\n3. Смешать яйца с тёртым сыром и сливками.\n4. Соединить всё, добавить воду от пасты для кремовости.",
                Calories = 450,
                PrepTime = 10,
                CookTime = 15,
                ImageUrl = ""
            },
            new()
            {
                Title = "Овсяная каша с ягодами",
                Description = "Полезный и вкусный завтрак с ягодами и мёдом. Готовится за 15 минут.",
                Instructions = "1. Залить овсянку молоком и варить 5-7 минут.\n2. Добавить ягоды и мёд.\n3. Перемешать и подавать.",
                Calories = 250,
                PrepTime = 5,
                CookTime = 10,
                ImageUrl = ""
            },
            new()
            {
                Title = "Греческий салат",
                Description = "Свежий салат с помидорами, огурцами, оливками и фетой. Заправляется оливковым маслом.",
                Instructions = "1. Нарезать помидоры и огурцы.\n2. Добавить оливки и фету.\n3. Заправить оливковым маслом и специями.",
                Calories = 180,
                PrepTime = 10,
                CookTime = 5,
                ImageUrl = ""
            },
            new()
            {
                Title = "Куриный суп",
                Description = "Наваристый суп с курицей, лапшой и овощами. Согреет в холодный день.",
                Instructions = "1. Отварить курицу до готовности.\n2. Добавить нарезанные овощи.\n3. Варить 20 минут.\n4. Добавить лапшу и варить ещё 5 минут.",
                Calories = 320,
                PrepTime = 15,
                CookTime = 30,
                ImageUrl = ""
            },
            new()
            {
                Title = "Омлет с овощами",
                Description = "Пышный омлет с помидорами, перцем и зеленью. Отличный завтрак за 10 минут.",
                Instructions = "1. Взбить яйца с солью.\n2. Обжарить нарезанные овощи.\n3. Залить яичной смесью.\n4. Готовить под крышкой 5-7 минут.",
                Calories = 220,
                PrepTime = 5,
                CookTime = 10,
                ImageUrl = ""
            },
            new()
            {
                Title = "Борщ",
                Description = "Традиционный борщ со сметаной и зеленью. Настоящая русская классика.",
                Instructions = "1. Сварить бульон из говядины.\n2. Обжарить лук, морковь, свёклу.\n3. Добавить капусту и картофель.\n4. Варить до готовности.",
                Calories = 350,
                PrepTime = 20,
                CookTime = 40,
                ImageUrl = ""
            },
            new()
            {
                Title = "Тирамису",
                Description = "Итальянский десерт с маскарпоне и кофе. Нежный и воздушный.",
                Instructions = "1. Сварить крепкий кофе.\n2. Взбить маскарпоне с сахаром и яйцами.\n3. Обмакнуть печенье в кофе.\n4. Выложить слоями крем и печенье.",
                Calories = 380,
                PrepTime = 20,
                CookTime = 30,
                ImageUrl = ""
            },
            new()
            {
                Title = "Лосось с овощами",
                Description = "Сочный лосось запечённый с лимоном, спаржей и помидорами.",
                Instructions = "1. Замариновать лосось в лимонном соке.\n2. Выложить на противень с овощами.\n3. Запекать 20 минут при 180°C.",
                Calories = 420,
                PrepTime = 10,
                CookTime = 20,
                ImageUrl = ""
            }
        };
        db.Recipes.AddRange(recipes);
        db.SaveChanges();

        var productDict = products.ToDictionary(p => p.Name, p => p.Id);

        var recipeIngredients = new List<RecipeIngredient>
{
    // Паста Карбонара (рецепт 1)
    new() { RecipeId = 1, ProductId = productDict["Спагетти"], Quantity = 200 },
    new() { RecipeId = 1, ProductId = productDict["Бекон"], Quantity = 100 },
    new() { RecipeId = 1, ProductId = productDict["Яйца"], Quantity = 2 },
    new() { RecipeId = 1, ProductId = productDict["Сыр"], Quantity = 50 },
    new() { RecipeId = 1, ProductId = productDict["Сливки"], Quantity = 100 },
    new() { RecipeId = 1, ProductId = productDict["Чеснок"], Quantity = 2 },
    
    // Овсяная каша (рецепт 2)
    new() { RecipeId = 2, ProductId = productDict["Овсянка"], Quantity = 50 },
    new() { RecipeId = 2, ProductId = productDict["Молоко"], Quantity = 200 },
    new() { RecipeId = 2, ProductId  = productDict["Яблоки"], Quantity = 1 },
    new() { RecipeId = 2, ProductId = productDict["Сахар"], Quantity = 10 },
    
    // Греческий салат (рецепт 3)
    new() { RecipeId = 3, ProductId = productDict["Помидоры"], Quantity = 2 },
    new() { RecipeId = 3, ProductId = productDict["Огурцы"], Quantity = 1 },
    new() { RecipeId = 3, ProductId = productDict["Сыр"], Quantity = 100 },
    new() { RecipeId = 3, ProductId = productDict["Оливковое масло"], Quantity = 30 },
    new() { RecipeId = 3, ProductId = productDict["Лимоны"], Quantity = 1 },
    
    // Куриный суп (рецепт 4)
    new() { RecipeId = 4, ProductId = productDict["Курица"], Quantity = 300 },
    new() { RecipeId = 4, ProductId = productDict["Картофель"], Quantity = 2 },
    new() { RecipeId = 4, ProductId = productDict["Морковь"], Quantity = 1 },
    new() { RecipeId = 4, ProductId = productDict["Лук"], Quantity = 1 },
    new() { RecipeId = 4, ProductId = productDict["Соль"], Quantity = 10 },
    
    // Омлет с овощами (рецепт 5)
    new() { RecipeId = 5, ProductId = productDict["Яйца"], Quantity = 3 },
    new() { RecipeId = 5, ProductId = productDict["Помидоры"], Quantity = 1 },
    new() { RecipeId = 5, ProductId = productDict["Болгарский перец"], Quantity = 1 },
    new() { RecipeId = 5, ProductId = productDict["Молоко"], Quantity = 50 },
    
    // Борщ (рецепт 6)
    new() { RecipeId = 6, ProductId = productDict["Говядина"], Quantity = 300 },
    new() { RecipeId = 6, ProductId = productDict["Картофель"], Quantity = 3 },
    new() { RecipeId = 6, ProductId = productDict["Морковь"], Quantity = 1 },
    new() { RecipeId = 6, ProductId = productDict["Лук"], Quantity = 1 },
    new() { RecipeId = 6, ProductId = productDict["Помидоры"], Quantity = 2 },
    
    // Тирамису (рецепт 7)
    new() { RecipeId = 7, ProductId = productDict["Яйца"], Quantity = 3 },
    new() { RecipeId = 7, ProductId = productDict["Сахар"], Quantity = 100 },
    new() { RecipeId = 7, ProductId = productDict["Мука"], Quantity = 200 },
    new() { RecipeId = 7, ProductId = productDict["Кофе"], Quantity = 30 },
    
    // Лосось с овощами (рецепт 8)
    new() { RecipeId = 8, ProductId = productDict["Помидоры"], Quantity = 2 },
    new() { RecipeId = 8, ProductId = productDict["Лимоны"], Quantity = 1 },
    new() { RecipeId = 8, ProductId = productDict["Оливковое масло"], Quantity = 20 },
    new() { RecipeId = 8, ProductId = productDict["Соль"], Quantity = 5 },
};
        db.RecipeIngredients.AddRange(recipeIngredients);
        db.SaveChanges();

        // === СПИСОК ПОКУПОК ===
        var shoppingList = new ShoppingList
        {
            UserId = admin.Id,
            Name = "Еженедельный",
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow,
        };
        db.ShoppingList.Add(shoppingList);
        db.SaveChanges();

        // Добавляем элементы списка покупок
        var shoppingListItems = new List<ShoppingListItem>
{
    new() { ShoppingListId = shoppingList.Id, ProductId  = productDict["Молоко"], Quantity = 2 },
    new() { ShoppingListId = shoppingList.Id, ProductId  = productDict["Спагетти"], Quantity = 1 },
    new() { ShoppingListId = shoppingList.Id, ProductId  = productDict["Яблоки"], Quantity = 5 },
    new() { ShoppingListId = shoppingList.Id, ProductId  = productDict["Яйца"], Quantity = 1 },
};
        db.ShoppingListItems.AddRange(shoppingListItems);
        db.SaveChanges();

        // === МЕНЮ ===
        var menu = new Menu
        {
            UserId = admin.Id,
            Name = "План на неделю",
            CreatedAt = DateTime.UtcNow,
            Items = new List<MenuItem>
            {
                new() { RecipeId = 2, Date = DateTime.UtcNow, MealType = "breakfast" },
                new() { RecipeId = 4, Date = DateTime.UtcNow, MealType = "lunch" },
                new() { RecipeId = 1, Date = DateTime.UtcNow, MealType = "dinner" },
                new() { RecipeId = 5, Date = DateTime.UtcNow.AddDays(1), MealType = "breakfast" },
                new() { RecipeId = 6, Date = DateTime.UtcNow.AddDays(1), MealType = "lunch" },
            }
        };
        db.Menus.Add(menu);
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
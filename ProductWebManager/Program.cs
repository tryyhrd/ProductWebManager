using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Classes.AI;
using ProductWebManager.Components;
using ProductWebManager.Data;
using ProductWebManager.Models;
using ProductWebManager.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
builder.Services.AddHttpClient<ProductWebManager.Services.OpenFoodFactsService>();
builder.Services.AddScoped<RecipeResolverService>();
builder.Services.AddScoped<MealPlanBalancerService>();
builder.Services.AddScoped<MealPlanGeneratorService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/api/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<ProductManagerContext>();
;
    //db.Database.EnsureCreated();

    //if (!db.Users.Any())
    //{
    //    var admin = new User
    //    {
    //        Login = "admin",
    //        Password = "admin",
    //        Profile = new UserProfile
    //        {
    //            Age = 28,
    //            Height = 182,
    //            Weight = 82,
    //            Gender = Gender.Male,
    //            ActivityLevel = ActivityLevel.Medium,
    //            Goal = GoalType.Maintain,
    //            TargetCalories = 2600
    //        }
    //    };
    //    var user = new User
    //    {
    //        Login = "user",
    //        Password = "user",
    //        Profile = new UserProfile
    //        {
    //            Age = 27,
    //            Height = 168,
    //            Weight = 61,
    //            Gender = Gender.Female,
    //            ActivityLevel = ActivityLevel.Low,
    //            Goal = GoalType.LoseWeight,
    //            TargetCalories = 1700
    //        }
    //    };
    //    db.Users.AddRange(admin, user);
    //    db.SaveChanges();

    //    // =========================================
    //    // ALLERGIES
    //    // =========================================
    //    var allergies = new List<Allergy>
    //    {
    //        new() { Name = "Лактоза" },
    //        new() { Name = "Глютен" },
    //        new() { Name = "Орехи" },
    //        new() { Name = "Рыба" },
    //        new() { Name = "Морепродукты" },
    //        new() { Name = "Яйца" },
    //        new() { Name = "Соя" },
    //    };
    //    db.Allergies.AddRange(allergies);
    //    db.SaveChanges();

    //    db.UserAllergies.AddRange(
    //        new UserAllergy { UserId = admin.Id, AllergyId = allergies[2].Id },
    //        new UserAllergy { UserId = user.Id, AllergyId = allergies[0].Id }
    //    );
    //    db.SaveChanges();

    //    // =========================================
    //    // UNITS
    //    // =========================================
    //    var uShts = new Unit { Name = "шт." };
    //    var uG    = new Unit { Name = "г" };
    //    var uMl   = new Unit { Name = "мл" };
    //    var uKg   = new Unit { Name = "кг" };
    //    var uL    = new Unit { Name = "л" };
    //    var uTsp  = new Unit { Name = "ч.л." };
    //    var uTbsp = new Unit { Name = "ст.л." };
    //    db.Units.AddRange(uShts, uG, uMl, uKg, uL, uTsp, uTbsp);
    //    db.SaveChanges();

    //    // =========================================
    //    // CATEGORIES
    //    // =========================================
    //    var catMeat    = new Category { Name = "Мясо и птица" };
    //    var catFish    = new Category { Name = "Рыба и морепродукты" };
    //    var catDairy   = new Category { Name = "Молочное и яйца" };
    //    var catGrains  = new Category { Name = "Злаки и крупы" };
    //    var catVeg     = new Category { Name = "Овощи" };
    //    var catFruits  = new Category { Name = "Фрукты и ягоды" };
    //    var catOils    = new Category { Name = "Масла и жиры" };
    //    var catSpices  = new Category { Name = "Специи и приправы" };
    //    var catLegumes = new Category { Name = "Бобовые" };
    //    var catNuts    = new Category { Name = "Орехи и семена" };
    //    var catMushrooms = new Category { Name = "Грибы" };
    //    var catBread   = new Category { Name = "Хлеб и выпечка" };
    //    db.Categories.AddRange(catMeat, catFish, catDairy, catGrains, catVeg,
    //                           catFruits, catOils, catSpices, catLegumes, catNuts,
    //                           catMushrooms, catBread);
    //    db.SaveChanges();

    //    // =========================================
    //    // PRODUCTS (~55 позиций, КБЖУ на 100г)
    //    // =========================================
    //    // Мясо и птица
    //    var chicken   = new Product { Name = "Куриная грудка", UnitId = uG.Id, CategoryId = catMeat.Id, Proteins = 23.6, Fats = 1.9, Carbohydrates = 0.4, Calories = 113, Price = 380, ImageUrl = "https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400" };
    //    var beef      = new Product { Name = "Говядина (вырезка)", UnitId = uG.Id, CategoryId = catMeat.Id, Proteins = 26.1, Fats = 7.0, Carbohydrates = 0, Calories = 172, Price = 700, ImageUrl = "https://images.unsplash.com/photo-1546833998-877b37c2e5c6?w=400" };
    //    var pork      = new Product { Name = "Свинина (вырезка)", UnitId = uG.Id, CategoryId = catMeat.Id, Proteins = 22.3, Fats = 5.5, Carbohydrates = 0, Calories = 142, Price = 480, ImageUrl = "https://images.unsplash.com/photo-1607623814075-e51df1bdc82f?w=400" };
    //    var turkey    = new Product { Name = "Индейка", UnitId = uG.Id, CategoryId = catMeat.Id, Proteins = 21.0, Fats = 4.5, Carbohydrates = 0, Calories = 125, Price = 420, ImageUrl = "https://images.unsplash.com/photo-1574672280600-4accfa5b6f98?w=400" };
    //    var chickenLeg = new Product { Name = "Куриное бедро", UnitId = uG.Id, CategoryId = catMeat.Id, Proteins = 21.3, Fats = 10.9, Carbohydrates = 0, Calories = 185, Price = 260, ImageUrl = "https://images.unsplash.com/photo-1626082927389-6cd097cdc6ec?w=400" };

    //    // Рыба
    //    var salmon    = new Product { Name = "Лосось", UnitId = uG.Id, CategoryId = catFish.Id, Proteins = 20.5, Fats = 13.4, Carbohydrates = 0, Calories = 208, Price = 1100, ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=400" };
    //    var cod       = new Product { Name = "Треска", UnitId = uG.Id, CategoryId = catFish.Id, Proteins = 18.5, Fats = 0.5, Carbohydrates = 0, Calories = 83, Price = 350, ImageUrl = "https://images.unsplash.com/photo-1562802378-063ec186a863?w=400" };
    //    var tuna      = new Product { Name = "Тунец (консервы)", UnitId = uG.Id, CategoryId = catFish.Id, Proteins = 25.5, Fats = 1.1, Carbohydrates = 0, Calories = 117, Price = 180, ImageUrl = "https://images.unsplash.com/photo-1611171711912-e3f1e5a6c5f1?w=400" };
    //    var shrimp    = new Product { Name = "Креветки", UnitId = uG.Id, CategoryId = catFish.Id, Proteins = 20.5, Fats = 1.3, Carbohydrates = 0, Calories = 99, Price = 900, ImageUrl = "https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=400" };

    //    // Молочное и яйца
    //    var egg       = new Product { Name = "Яйцо куриное", UnitId = uShts.Id, CategoryId = catDairy.Id, Proteins = 12.7, Fats = 10.9, Carbohydrates = 0.7, Calories = 157, Price = 10, IsPieceBased = true, AverageWeightGrams = 60, ImageUrl = "https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400" };
    //    var milk      = new Product { Name = "Молоко 2.5%", UnitId = uMl.Id, CategoryId = catDairy.Id, Proteins = 2.8, Fats = 2.5, Carbohydrates = 4.7, Calories = 52, Price = 90, ImageUrl = "https://images.unsplash.com/photo-1550583724-b2692b85b150?w=400" };
    //    var kefir     = new Product { Name = "Кефир 1%", UnitId = uMl.Id, CategoryId = catDairy.Id, Proteins = 3.0, Fats = 1.0, Carbohydrates = 4.0, Calories = 40, Price = 75, ImageUrl = "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=400" };
    //    var cottage   = new Product { Name = "Творог 5%", UnitId = uG.Id, CategoryId = catDairy.Id, Proteins = 17.2, Fats = 5.0, Carbohydrates = 1.8, Calories = 121, Price = 160, ImageUrl = "https://images.unsplash.com/photo-1630409351217-bc4fa6422075?w=400" };
    //    var cheese    = new Product { Name = "Сыр Российский", UnitId = uG.Id, CategoryId = catDairy.Id, Proteins = 23.0, Fats = 29.0, Carbohydrates = 0.3, Calories = 360, Price = 250, ImageUrl = "https://images.unsplash.com/photo-1486297678162-eb2a19b0a32d?w=400" };
    //    var butter    = new Product { Name = "Сливочное масло", UnitId = uG.Id, CategoryId = catDairy.Id, Proteins = 0.5, Fats = 82.5, Carbohydrates = 0.8, Calories = 748, Price = 120, ImageUrl = "https://images.unsplash.com/photo-1558642452-9d2a7deb7f62?w=400" };
    //    var smetana   = new Product { Name = "Сметана 15%", UnitId = uG.Id, CategoryId = catDairy.Id, Proteins = 2.6, Fats = 15.0, Carbohydrates = 3.0, Calories = 158, Price = 100, ImageUrl = "https://images.unsplash.com/photo-1612481418929-b4c4b8fc9b36?w=400" };

    //    // Злаки и крупы
    //    var rice      = new Product { Name = "Рис белый", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 6.7, Fats = 0.7, Carbohydrates = 78.9, Calories = 344, Price = 100, ImageUrl = "https://images.unsplash.com/photo-1516684732162-798a0062be99?w=400" };
    //    var buckwheat = new Product { Name = "Гречневая крупа", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 13.3, Fats = 3.4, Carbohydrates = 68.5, Calories = 343, Price = 120, ImageUrl = "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400" };
    //    var oats      = new Product { Name = "Овсяные хлопья", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 12.3, Fats = 6.1, Carbohydrates = 67.5, Calories = 371, Price = 80, ImageUrl = "https://images.unsplash.com/photo-1614961908766-4c65cd8b1a0f?w=400" };
    //    var pasta     = new Product { Name = "Макароны", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 11.4, Fats = 1.4, Carbohydrates = 72.8, Calories = 344, Price = 90, ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?w=400" };
    //    var flour     = new Product { Name = "Мука пшеничная", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 10.3, Fats = 1.1, Carbohydrates = 73.2, Calories = 342, Price = 60, ImageUrl = "https://images.unsplash.com/photo-1574323347407-f5e1ad6d020b?w=400" };
    //    var bulgur    = new Product { Name = "Булгур", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 12.3, Fats = 1.3, Carbohydrates = 65.2, Calories = 342, Price = 150, ImageUrl = "https://images.unsplash.com/photo-1591348122449-02525d70379b?w=400" };
    //    var millet    = new Product { Name = "Пшено", UnitId = uG.Id, CategoryId = catGrains.Id, Proteins = 11.5, Fats = 3.3, Carbohydrates = 66.5, Calories = 348, Price = 70, ImageUrl = "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=400" };

    //    // Овощи
    //    var potato    = new Product { Name = "Картофель", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 2.0, Fats = 0.1, Carbohydrates = 18.1, Calories = 83, Price = 40, ImageUrl = "https://images.unsplash.com/photo-1518977676601-b53f82aba655?w=400" };
    //    var carrot    = new Product { Name = "Морковь", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.3, Fats = 0.1, Carbohydrates = 9.3, Calories = 43, Price = 30, ImageUrl = "https://images.unsplash.com/photo-1447175008436-054170c2e979?w=400" };
    //    var onion     = new Product { Name = "Лук репчатый", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.4, Fats = 0.2, Carbohydrates = 10.4, Calories = 47, Price = 25, ImageUrl = "https://images.unsplash.com/photo-1587735243615-c03f25aaff15?w=400" };
    //    var garlic    = new Product { Name = "Чеснок", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 6.5, Fats = 0.5, Carbohydrates = 30.0, Calories = 149, Price = 150, ImageUrl = "https://images.unsplash.com/photo-1501420193013-c2e6e57afe7e?w=400" };
    //    var tomato    = new Product { Name = "Помидоры", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.1, Fats = 0.2, Carbohydrates = 5.0, Calories = 24, Price = 150, ImageUrl = "https://images.unsplash.com/photo-1546094096-0df4bcaaa337?w=400" };
    //    var cucumber  = new Product { Name = "Огурцы", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 0.8, Fats = 0.1, Carbohydrates = 3.0, Calories = 16, Price = 100, ImageUrl = "https://images.unsplash.com/photo-1449300079323-02e209d9d3a6?w=400" };
    //    var pepper    = new Product { Name = "Болгарский перец", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.3, Fats = 0.3, Carbohydrates = 8.1, Calories = 40, Price = 180, ImageUrl = "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?w=400" };
    //    var cabbage   = new Product { Name = "Капуста белокочанная", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.8, Fats = 0.1, Carbohydrates = 6.8, Calories = 30, Price = 35, ImageUrl = "https://images.unsplash.com/photo-1594282486552-05b4d80fbb9f?w=400" };
    //    var broccoli  = new Product { Name = "Брокколи", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 2.8, Fats = 0.4, Carbohydrates = 6.6, Calories = 43, Price = 200, ImageUrl = "https://images.unsplash.com/photo-1459411621453-7b03977f4bfc?w=400" };
    //    var spinach   = new Product { Name = "Шпинат", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 2.9, Fats = 0.4, Carbohydrates = 3.6, Calories = 29, Price = 120, ImageUrl = "https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400" };
    //    var eggplant  = new Product { Name = "Баклажан", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.2, Fats = 0.1, Carbohydrates = 7.1, Calories = 35, Price = 140, ImageUrl = "https://images.unsplash.com/photo-1614859185247-e4e39b7e2b1e?w=400" };
    //    var zucchini  = new Product { Name = "Кабачок", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 0.6, Fats = 0.3, Carbohydrates = 4.9, Calories = 27, Price = 90, ImageUrl = "https://images.unsplash.com/photo-1563252722-6434563a985d?w=400" };
    //    var beet      = new Product { Name = "Свёкла", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 1.5, Fats = 0.1, Carbohydrates = 11.8, Calories = 54, Price = 30, ImageUrl = "https://images.unsplash.com/photo-1595855759920-86582396756a?w=400" };
    //    var tomatoPaste = new Product { Name = "Томатная паста", UnitId = uG.Id, CategoryId = catVeg.Id, Proteins = 4.8, Fats = 0.5, Carbohydrates = 18.9, Calories = 100, Price = 60, ImageUrl = "https://images.unsplash.com/photo-1578020190125-f4f7c18bc9cb?w=400" };

    //    // Фрукты
    //    var apple     = new Product { Name = "Яблоко", UnitId = uShts.Id, CategoryId = catFruits.Id, Proteins = 0.4, Fats = 0.4, Carbohydrates = 13.8, Calories = 54, Price = 80, IsPieceBased = true, AverageWeightGrams = 150, ImageUrl = "https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400" };
    //    var banana    = new Product { Name = "Банан", UnitId = uShts.Id, CategoryId = catFruits.Id, Proteins = 1.5, Fats = 0.2, Carbohydrates = 21.8, Calories = 96, Price = 90, IsPieceBased = true, AverageWeightGrams = 130, ImageUrl = "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400" };
    //    var orange    = new Product { Name = "Апельсин", UnitId = uShts.Id, CategoryId = catFruits.Id, Proteins = 0.9, Fats = 0.2, Carbohydrates = 11.8, Calories = 53, Price = 100, IsPieceBased = true, AverageWeightGrams = 200, ImageUrl = "https://images.unsplash.com/photo-1547514701-42782101795e?w=400" };
    //    var strawberry = new Product { Name = "Клубника", UnitId = uG.Id, CategoryId = catFruits.Id, Proteins = 0.8, Fats = 0.4, Carbohydrates = 9.3, Calories = 45, Price = 400, ImageUrl = "https://images.unsplash.com/photo-1464965911861-746a04b4bca6?w=400" };
    //    var lemon     = new Product { Name = "Лимон", UnitId = uShts.Id, CategoryId = catFruits.Id, Proteins = 0.9, Fats = 0.1, Carbohydrates = 9.3, Calories = 43, Price = 30, IsPieceBased = true, AverageWeightGrams = 120, ImageUrl = "https://images.unsplash.com/photo-1582476657734-796f977f6325?w=400" };

    //    // Масла
    //    var sunflowerOil = new Product { Name = "Подсолнечное масло", UnitId = uMl.Id, CategoryId = catOils.Id, Proteins = 0, Fats = 99.9, Carbohydrates = 0, Calories = 899, Price = 120, ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400" };
    //    var oliveOil  = new Product { Name = "Оливковое масло", UnitId = uMl.Id, CategoryId = catOils.Id, Proteins = 0, Fats = 99.8, Carbohydrates = 0, Calories = 898, Price = 600, ImageUrl = "https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400" };

    //    // Специи
    //    var soySauce  = new Product { Name = "Соевый соус", UnitId = uMl.Id, CategoryId = catSpices.Id, Proteins = 8.1, Fats = 0.1, Carbohydrates = 8.4, Calories = 73, Price = 130, ImageUrl = "https://images.unsplash.com/photo-1582281298055-e25b84a30b0b?w=400" };
    //    var salt      = new Product { Name = "Соль", UnitId = uG.Id, CategoryId = catSpices.Id, Proteins = 0, Fats = 0, Carbohydrates = 0, Calories = 0, Price = 20, ImageUrl = "https://images.unsplash.com/photo-1518110925495-5fe2fda0442c?w=400" };
    //    var blackPepper = new Product { Name = "Перец чёрный молотый", UnitId = uG.Id, CategoryId = catSpices.Id, Proteins = 10.4, Fats = 3.3, Carbohydrates = 64.0, Calories = 251, Price = 80, ImageUrl = "https://images.unsplash.com/photo-1600565193348-f74bd3c7ccdf?w=400" };

    //    // Бобовые
    //    var redBeans  = new Product { Name = "Фасоль красная (варёная)", UnitId = uG.Id, CategoryId = catLegumes.Id, Proteins = 8.4, Fats = 0.5, Carbohydrates = 21.5, Calories = 127, Price = 120, ImageUrl = "https://images.unsplash.com/photo-1615485290382-441e4d049cb5?w=400" };
    //    var lentils   = new Product { Name = "Чечевица", UnitId = uG.Id, CategoryId = catLegumes.Id, Proteins = 24.0, Fats = 1.5, Carbohydrates = 53.7, Calories = 295, Price = 130, ImageUrl = "https://images.unsplash.com/photo-1571200174957-f12d61f4f24e?w=400" };
    //    var chickpea  = new Product { Name = "Нут (варёный)", UnitId = uG.Id, CategoryId = catLegumes.Id, Proteins = 9.0, Fats = 2.6, Carbohydrates = 27.4, Calories = 164, Price = 160, ImageUrl = "https://images.unsplash.com/photo-1515543237350-b3eea1ec8082?w=400" };

    //    // Орехи
    //    var walnut    = new Product { Name = "Грецкий орех", UnitId = uG.Id, CategoryId = catNuts.Id, Proteins = 15.2, Fats = 65.2, Carbohydrates = 13.7, Calories = 687, Price = 800, ImageUrl = "https://images.unsplash.com/photo-1563409236986-bbd8c2a7ccdd?w=400" };
    //    var almond    = new Product { Name = "Миндаль", UnitId = uG.Id, CategoryId = catNuts.Id, Proteins = 21.3, Fats = 57.7, Carbohydrates = 13.0, Calories = 645, Price = 900, ImageUrl = "https://images.unsplash.com/photo-1574570068036-d3b60d4b4a47?w=400" };

    //    // Грибы
    //    var champignon = new Product { Name = "Шампиньоны", UnitId = uG.Id, CategoryId = catMushrooms.Id, Proteins = 4.3, Fats = 1.0, Carbohydrates = 0.1, Calories = 27, Price = 200, ImageUrl = "https://images.unsplash.com/photo-1552825897-bb4eda57a6d5?w=400" };

    //    // Хлеб
    //    var whiteBread = new Product { Name = "Хлеб белый", UnitId = uG.Id, CategoryId = catBread.Id, Proteins = 8.0, Fats = 1.5, Carbohydrates = 48.8, Calories = 242, Price = 50, ImageUrl = "https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400" };
    //    var ryeBread  = new Product { Name = "Хлеб ржаной", UnitId = uG.Id, CategoryId = catBread.Id, Proteins = 6.9, Fats = 1.3, Carbohydrates = 41.8, Calories = 214, Price = 60, ImageUrl = "https://images.unsplash.com/photo-1486887397853-03c92e5c9c10?w=400" };

    //    db.Products.AddRange(
    //        chicken, beef, pork, turkey, chickenLeg,
    //        salmon, cod, tuna, shrimp,
    //        egg, milk, kefir, cottage, cheese, butter, smetana,
    //        rice, buckwheat, oats, pasta, flour, bulgur, millet,
    //        potato, carrot, onion, garlic, tomato, cucumber, pepper,
    //        cabbage, broccoli, spinach, eggplant, zucchini, beet, tomatoPaste,
    //        apple, banana, orange, strawberry, lemon,
    //        sunflowerOil, oliveOil,
    //        soySauce, salt, blackPepper,
    //        redBeans, lentils, chickpea,
    //        walnut, almond,
    //        champignon,
    //        whiteBread, ryeBread
    //    );
    //    db.SaveChanges();

    //    // Аллергии продуктов
    //    db.ProductAllergies.AddRange(
    //        new ProductAllergy { ProductId = milk.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = kefir.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = cottage.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = cheese.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = smetana.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = butter.Id, AllergyId = allergies[0].Id },
    //        new ProductAllergy { ProductId = flour.Id, AllergyId = allergies[1].Id },
    //        new ProductAllergy { ProductId = whiteBread.Id, AllergyId = allergies[1].Id },
    //        new ProductAllergy { ProductId = pasta.Id, AllergyId = allergies[1].Id },
    //        new ProductAllergy { ProductId = oats.Id, AllergyId = allergies[1].Id },
    //        new ProductAllergy { ProductId = walnut.Id, AllergyId = allergies[2].Id },
    //        new ProductAllergy { ProductId = almond.Id, AllergyId = allergies[2].Id },
    //        new ProductAllergy { ProductId = salmon.Id, AllergyId = allergies[3].Id },
    //        new ProductAllergy { ProductId = cod.Id, AllergyId = allergies[3].Id },
    //        new ProductAllergy { ProductId = tuna.Id, AllergyId = allergies[3].Id },
    //        new ProductAllergy { ProductId = shrimp.Id, AllergyId = allergies[4].Id },
    //        new ProductAllergy { ProductId = egg.Id, AllergyId = allergies[5].Id },
    //        new ProductAllergy { ProductId = soySauce.Id, AllergyId = allergies[6].Id }
    //    );
    //    db.SaveChanges();

    //    // =========================================
    //    // FRIDGE (наполняем холодильник admin)
    //    // =========================================
    //    db.FridgeItems.AddRange(
    //        new FridgeItem { UserId = admin.Id, ProductId = chicken.Id, Quantity = 700, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(3) },
    //        new FridgeItem { UserId = admin.Id, ProductId = egg.Id, Quantity = 10, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(14) },
    //        new FridgeItem { UserId = admin.Id, ProductId = milk.Id, Quantity = 1000, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(5) },
    //        new FridgeItem { UserId = admin.Id, ProductId = rice.Id, Quantity = 500, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(365) },
    //        new FridgeItem { UserId = admin.Id, ProductId = buckwheat.Id, Quantity = 400, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(365) },
    //        new FridgeItem { UserId = admin.Id, ProductId = tomato.Id, Quantity = 400, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(7) },
    //        new FridgeItem { UserId = admin.Id, ProductId = onion.Id, Quantity = 500, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(30) },
    //        new FridgeItem { UserId = admin.Id, ProductId = garlic.Id, Quantity = 100, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(60) },
    //        new FridgeItem { UserId = admin.Id, ProductId = potato.Id, Quantity = 1000, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(20) },
    //        new FridgeItem { UserId = admin.Id, ProductId = carrot.Id, Quantity = 500, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(21) },
    //        new FridgeItem { UserId = admin.Id, ProductId = sunflowerOil.Id, Quantity = 900, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(180) },
    //        new FridgeItem { UserId = admin.Id, ProductId = oats.Id, Quantity = 500, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(365) },
    //        new FridgeItem { UserId = admin.Id, ProductId = cottage.Id, Quantity = 200, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(4) },
    //        new FridgeItem { UserId = admin.Id, ProductId = banana.Id, Quantity = 3, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(5) },
    //        new FridgeItem { UserId = admin.Id, ProductId = beet.Id, Quantity = 300, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(25) },
    //        new FridgeItem { UserId = admin.Id, ProductId = cabbage.Id, Quantity = 800, AddedAt = DateTime.UtcNow, ExpirationDate = DateTime.UtcNow.AddDays(14) }
    //    );
    //    db.SaveChanges();

    //    // =========================================
    //    // RECIPES (~20 рецептов)
    //    // =========================================
    //    var r1 = new Recipe { Title = "Куриная грудка с рисом", Description = "Классическое и сытное блюдо с высоким содержанием белка. Идеально для тех, кто следит за питанием.", Instructions = "1. Рис промыть и сварить в подсоленной воде (1:2) до готовности (~20 мин).\n2. Куриную грудку нарезать тонкими пластинами, посолить и поперчить.\n3. Обжарить курицу на раскалённой сковороде с маслом по 4-5 минут с каждой стороны.\n4. Подавать курицу на подушке из риса, украсить зеленью.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800" };
    //    var r2 = new Recipe { Title = "Греческий салат", Description = "Лёгкий и свежий салат со средиземноморским вкусом. Минимум калорий, максимум пользы.", Instructions = "1. Помидоры и огурцы нарезать крупными кубиками.\n2. Болгарский перец нарезать полукольцами.\n3. Смешать всё в миске, добавить оливки.\n4. Заправить оливковым маслом, солью и перцем.\n5. По желанию добавить сыр Фета.", PrepTime = 15, CookTime = 0, ImageUrl = "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=800" };
    //    var r3 = new Recipe { Title = "Борщ классический", Description = "Наваристый и ароматный борщ с кусочками свёклы и капустой. Настоящий домашний вкус.", Instructions = "1. Сварить мясной бульон (говядина, 1.5 часа).\n2. Свёклу натереть на тёрке и потушить на сковороде с небольшим количеством масла и уксуса (~15 мин).\n3. Морковь и лук обжарить на сковороде до золотистости.\n4. В кипящий бульон добавить картофель, через 10 мин — капусту.\n5. Через 5 мин добавить зажарку, свёклу и томатную пасту.\n6. Варить ещё 10 минут, добавить чеснок и зелень.", PrepTime = 20, CookTime = 90, ImageUrl = "https://images.unsplash.com/photo-1548943487-a2e4e43b4853?w=800" };
    //    var r4 = new Recipe { Title = "Овсяная каша с бананом", Description = "Питательный и быстрый завтрак. Обеспечивает долгое насыщение и даёт энергию на весь день.", Instructions = "1. Залить овсяные хлопья горячим молоком в соотношении 1:2.\n2. Варить на медленном огне 5-7 минут, постоянно помешивая.\n3. Добавить щепотку соли и чайную ложку мёда (по желанию).\n4. Банан нарезать кружочками и выложить сверху.", PrepTime = 3, CookTime = 7, ImageUrl = "https://images.unsplash.com/photo-1517673400267-0251440c45dc?w=800" };
    //    var r5 = new Recipe { Title = "Омлет с овощами", Description = "Быстрый белковый завтрак или ужин с хрустящими овощами.", Instructions = "1. Яйца взбить с молоком, посолить и поперчить.\n2. Болгарский перец и помидоры нарезать мелким кубиком.\n3. Овощи обжарить на сковороде на масле 3-4 минуты.\n4. Залить яичной смесью и готовить под крышкой на медленном огне 5-7 минут.", PrepTime = 10, CookTime = 10, ImageUrl = "https://images.unsplash.com/photo-1510693206972-df098062cb71?w=800" };
    //    var r6 = new Recipe { Title = "Запечённый лосось", Description = "Нежный и ароматный лосось, запечённый с лимоном и пряными травами. Богат омега-3.", Instructions = "1. Лосось промыть, обсушить, посолить и поперчить.\n2. Смазать оливковым маслом, полить лимонным соком.\n3. Завернуть в фольгу с веточками тимьяна.\n4. Запекать в духовке при 180°C 20-25 минут.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=800" };
    //    var r7 = new Recipe { Title = "Гречка с курицей", Description = "Простое и питательное блюдо с гречневой крупой и нежной куриной грудкой.", Instructions = "1. Гречку промыть и сварить в подсоленной воде (1:2, 20 мин).\n2. Куриную грудку нарезать кубиками и обжарить на масле 7-10 минут.\n3. Лук и морковь мелко нарезать и обжарить до мягкости.\n4. Смешать всё вместе, добавить соль и перец.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1626203843219-82476b2f4a27?w=800" };
    //    var r8 = new Recipe { Title = "Паста с томатным соусом", Description = "Итальянская классика: сочный томатный соус с чесноком и базиликом на идеально отваренной пасте.", Instructions = "1. Пасту отварить в подсоленной воде по инструкции на упаковке.\n2. Чеснок измельчить и обжарить на оливковом масле 1 минуту.\n3. Добавить помидоры (или томатную пасту), тушить 10-15 минут.\n4. Приправить солью, перцем, базиликом.\n5. Смешать с пастой, подавать горячим.", PrepTime = 5, CookTime = 20, ImageUrl = "https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?w=800" };
    //    var r9 = new Recipe { Title = "Творожная запеканка", Description = "Нежная и воздушная запеканка из творога. Отличный вариант завтрака или полезного десерта.", Instructions = "1. Творог смешать с яйцами, мукой, сахаром и ванилью.\n2. Хорошо перемешать до однородности.\n3. Вылить в смазанную форму.\n4. Запекать при 180°C около 35-40 минут до золотистой корочки.\n5. Дать остыть 10 минут перед подачей.", PrepTime = 15, CookTime = 40, ImageUrl = "https://images.unsplash.com/photo-1602351447937-745cb720612f?w=800" };
    //    var r10 = new Recipe { Title = "Крем-суп из брокколи", Description = "Нежный и бархатистый суп с ярким вкусом брокколи. Лёгкий, полезный, согревающий.", Instructions = "1. Брокколи разобрать на соцветия, картофель нарезать кубиками.\n2. Варить в подсоленной воде 15 минут до мягкости.\n3. Слить часть воды, пробить блендером до кремовой консистенции.\n4. Добавить сливки или молоко, прогреть.\n5. Подавать со сметаной и гренками.", PrepTime = 10, CookTime = 20, ImageUrl = "https://images.unsplash.com/photo-1543826173-1beeb97525d8?w=800" };
    //    var r11 = new Recipe { Title = "Тушёная говядина с морковью", Description = "Нежная говядина, тушённая до мягкости с овощами. Наваристое и сытное блюдо.", Instructions = "1. Говядину нарезать кубиками 3x3 см, обжарить на сильном огне до корочки.\n2. Лук и морковь нарезать, добавить к мясу, обжарить 5 минут.\n3. Добавить томатную пасту, горячую воду, соль и специи.\n4. Тушить на медленном огне под крышкой 60-80 минут до мягкости мяса.", PrepTime = 15, CookTime = 80, ImageUrl = "https://images.unsplash.com/photo-1603360946369-dc9bb6258143?w=800" };
    //    var r12 = new Recipe { Title = "Фасолевый суп", Description = "Сытный и ароматный суп на основе красной фасоли с овощами.", Instructions = "1. Лук, морковь и чеснок обжарить на масле 5 минут.\n2. Добавить картофель кубиками, залить бульоном (1.5 л).\n3. Через 10 минут добавить красную фасоль и томатную пасту.\n4. Варить ещё 15 минут, добавить специи и зелень.", PrepTime = 15, CookTime = 35, ImageUrl = "https://images.unsplash.com/photo-1580013759032-c96505e24c1f?w=800" };
    //    var r13 = new Recipe { Title = "Салат с тунцом", Description = "Быстрый и белковый салат с консервированным тунцом. Готовится за 10 минут.", Instructions = "1. Тунец из консервы размять вилкой.\n2. Помидоры и огурцы нарезать кубиками.\n3. Смешать всё, добавить зелёный лук.\n4. Заправить оливковым маслом, солью и перцем.\n5. По желанию добавить варёные яйца.", PrepTime = 10, CookTime = 0, ImageUrl = "https://images.unsplash.com/photo-1512621776951-a57141f2eefd?w=800" };
    //    var r14 = new Recipe { Title = "Куриный суп", Description = "Лёгкий и питательный куриный суп с овощами. Идеален для восстановления.", Instructions = "1. Сварить куриный бульон из грудки (30-40 минут).\n2. Курицу вынуть, нарезать кусочками.\n3. В кипящий бульон добавить морковь, лук и картофель кубиками.\n4. Варить 20 минут, вернуть курицу, посолить, добавить зелень.", PrepTime = 10, CookTime = 50, ImageUrl = "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=800" };
    //    var r15 = new Recipe { Title = "Рататуй", Description = "Классическое французское рагу из овощей. Вегетарианское блюдо с насыщенным вкусом.", Instructions = "1. Кабачок, баклажан, помидоры нарезать кружочками.\n2. Болгарский перец и лук нарезать, обжарить на масле.\n3. Добавить томатную пасту и чеснок, тушить 5 минут.\n4. Выложить в форму слоями овощи, полить соусом.\n5. Запекать при 180°C 45 минут.", PrepTime = 20, CookTime = 50, ImageUrl = "https://images.unsplash.com/photo-1572453800999-e8d2d1589b7c?w=800" };
    //    var r16 = new Recipe { Title = "Чечевичный суп", Description = "Густой и наваристый суп из красной чечевицы. Богат растительным белком.", Instructions = "1. Лук, морковь и чеснок обжарить на масле до мягкости.\n2. Добавить чечевицу (промытую), залить горячей водой (1:3).\n3. Добавить томатную пасту, специи (куркума, тмин).\n4. Варить 25-30 минут до полного разваривания чечевицы.\n5. Пробить блендером часть супа для густоты.", PrepTime = 10, CookTime = 35, ImageUrl = "https://images.unsplash.com/photo-1516684669134-de6f7c473a2a?w=800" };
    //    var r17 = new Recipe { Title = "Куриные котлеты", Description = "Нежные домашние котлеты из куриного фарша. Любимое блюдо всей семьи.", Instructions = "1. Куриную грудку перемолоть в фарш или купить готовый.\n2. Добавить яйцо, измельчённый лук, соль, перец.\n3. Тщательно перемешать, сформировать котлеты.\n4. Обжарить на масле по 4-5 минут с каждой стороны.\n5. Довести до готовности под крышкой на медленном огне 5 минут.", PrepTime = 20, CookTime = 20, ImageUrl = "https://images.unsplash.com/photo-1614777735417-4959b46e9799?w=800" };
    //    var r18 = new Recipe { Title = "Тушёный кабачок с томатами", Description = "Лёгкое летнее блюдо из тушёного кабачка. Минимум калорий и максимум вкуса.", Instructions = "1. Кабачок нарезать кубиками.\n2. Лук и чеснок обжарить на масле.\n3. Добавить помидоры и кабачок.\n4. Тушить под крышкой 20-25 минут.\n5. Посолить, поперчить, добавить зелень.", PrepTime = 10, CookTime = 25, ImageUrl = "https://images.unsplash.com/photo-1563565375-f3fdfdbefa83?w=800" };
    //    var r19 = new Recipe { Title = "Яичница с помидорами", Description = "Классическая яичница с сочными томатами. Быстрый завтрак за 10 минут.", Instructions = "1. На сковороде разогреть масло.\n2. Помидоры нарезать кубиками, обжарить 2-3 минуты.\n3. Разбить яйца прямо на помидоры.\n4. Посолить, поперчить, жарить до желаемой готовности желтка.", PrepTime = 5, CookTime = 8, ImageUrl = "https://images.unsplash.com/photo-1525351484163-7529414344d8?w=800" };
    //    var r20 = new Recipe { Title = "Говяжий суп с картофелем", Description = "Наваристый суп на говяжьем бульоне с сытными овощами.", Instructions = "1. Говядину нарезать кусочками и сварить бульон (1 час).\n2. Картофель, морковь и лук нарезать.\n3. Добавить овощи в кипящий бульон.\n4. Варить 20-25 минут.\n5. Добавить чеснок, зелень, приправить.", PrepTime = 15, CookTime = 90, ImageUrl = "https://images.unsplash.com/photo-1615361200141-f45040f367be?w=800" };

    //    db.Recipes.AddRange(r1,r2,r3,r4,r5,r6,r7,r8,r9,r10,r11,r12,r13,r14,r15,r16,r17,r18,r19,r20);
    //    db.SaveChanges();

    //    // =========================================
    //    // RECIPE INGREDIENTS
    //    // =========================================
    //    db.RecipeIngredients.AddRange(
    //        // r1: Куриная грудка с рисом
    //        new RecipeIngredient { RecipeId = r1.Id, ProductId = chicken.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r1.Id, ProductId = rice.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r1.Id, ProductId = sunflowerOil.Id, Quantity = 15, UnitId = uMl.Id },
    //        new RecipeIngredient { RecipeId = r1.Id, ProductId = salt.Id, Quantity = 3, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r1.Id, ProductId = blackPepper.Id, Quantity = 2, UnitId = uG.Id },

    //        // r2: Греческий салат
    //        new RecipeIngredient { RecipeId = r2.Id, ProductId = tomato.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r2.Id, ProductId = cucumber.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r2.Id, ProductId = pepper.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r2.Id, ProductId = oliveOil.Id, Quantity = 30, UnitId = uMl.Id },

    //        // r3: Борщ
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = beef.Id, Quantity = 400, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = beet.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = cabbage.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = potato.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = carrot.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = onion.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = tomatoPaste.Id, Quantity = 40, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r3.Id, ProductId = garlic.Id, Quantity = 15, UnitId = uG.Id },

    //        // r4: Овсянка с бананом
    //        new RecipeIngredient { RecipeId = r4.Id, ProductId = oats.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r4.Id, ProductId = milk.Id, Quantity = 200, UnitId = uMl.Id },
    //        new RecipeIngredient { RecipeId = r4.Id, ProductId = banana.Id, Quantity = 1, UnitId = uShts.Id },

    //        // r5: Омлет с овощами
    //        new RecipeIngredient { RecipeId = r5.Id, ProductId = egg.Id, Quantity = 3, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r5.Id, ProductId = milk.Id, Quantity = 50, UnitId = uMl.Id },
    //        new RecipeIngredient { RecipeId = r5.Id, ProductId = pepper.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r5.Id, ProductId = tomato.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r5.Id, ProductId = sunflowerOil.Id, Quantity = 10, UnitId = uMl.Id },

    //        // r6: Запечённый лосось
    //        new RecipeIngredient { RecipeId = r6.Id, ProductId = salmon.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r6.Id, ProductId = lemon.Id, Quantity = 1, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r6.Id, ProductId = oliveOil.Id, Quantity = 20, UnitId = uMl.Id },

    //        // r7: Гречка с курицей
    //        new RecipeIngredient { RecipeId = r7.Id, ProductId = buckwheat.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r7.Id, ProductId = chicken.Id, Quantity = 250, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r7.Id, ProductId = onion.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r7.Id, ProductId = carrot.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r7.Id, ProductId = sunflowerOil.Id, Quantity = 20, UnitId = uMl.Id },

    //        // r8: Паста с томатным соусом
    //        new RecipeIngredient { RecipeId = r8.Id, ProductId = pasta.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r8.Id, ProductId = tomato.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r8.Id, ProductId = garlic.Id, Quantity = 20, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r8.Id, ProductId = oliveOil.Id, Quantity = 30, UnitId = uMl.Id },

    //        // r9: Творожная запеканка
    //        new RecipeIngredient { RecipeId = r9.Id, ProductId = cottage.Id, Quantity = 500, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r9.Id, ProductId = egg.Id, Quantity = 3, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r9.Id, ProductId = flour.Id, Quantity = 50, UnitId = uG.Id },

    //        // r10: Крем-суп из брокколи
    //        new RecipeIngredient { RecipeId = r10.Id, ProductId = broccoli.Id, Quantity = 400, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r10.Id, ProductId = potato.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r10.Id, ProductId = milk.Id, Quantity = 200, UnitId = uMl.Id },
    //        new RecipeIngredient { RecipeId = r10.Id, ProductId = onion.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r10.Id, ProductId = butter.Id, Quantity = 20, UnitId = uG.Id },

    //        // r11: Тушёная говядина с морковью
    //        new RecipeIngredient { RecipeId = r11.Id, ProductId = beef.Id, Quantity = 500, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r11.Id, ProductId = carrot.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r11.Id, ProductId = onion.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r11.Id, ProductId = tomatoPaste.Id, Quantity = 60, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r11.Id, ProductId = garlic.Id, Quantity = 15, UnitId = uG.Id },

    //        // r12: Фасолевый суп
    //        new RecipeIngredient { RecipeId = r12.Id, ProductId = redBeans.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r12.Id, ProductId = potato.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r12.Id, ProductId = carrot.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r12.Id, ProductId = onion.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r12.Id, ProductId = tomatoPaste.Id, Quantity = 40, UnitId = uG.Id },

    //        // r13: Салат с тунцом
    //        new RecipeIngredient { RecipeId = r13.Id, ProductId = tuna.Id, Quantity = 185, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r13.Id, ProductId = tomato.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r13.Id, ProductId = cucumber.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r13.Id, ProductId = egg.Id, Quantity = 2, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r13.Id, ProductId = oliveOil.Id, Quantity = 20, UnitId = uMl.Id },

    //        // r14: Куриный суп
    //        new RecipeIngredient { RecipeId = r14.Id, ProductId = chicken.Id, Quantity = 400, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r14.Id, ProductId = potato.Id, Quantity = 250, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r14.Id, ProductId = carrot.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r14.Id, ProductId = onion.Id, Quantity = 80, UnitId = uG.Id },

    //        // r15: Рататуй
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = zucchini.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = eggplant.Id, Quantity = 250, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = tomato.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = pepper.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = onion.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = garlic.Id, Quantity = 20, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = oliveOil.Id, Quantity = 40, UnitId = uMl.Id },
    //        new RecipeIngredient { RecipeId = r15.Id, ProductId = tomatoPaste.Id, Quantity = 50, UnitId = uG.Id },

    //        // r16: Чечевичный суп
    //        new RecipeIngredient { RecipeId = r16.Id, ProductId = lentils.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r16.Id, ProductId = onion.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r16.Id, ProductId = carrot.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r16.Id, ProductId = garlic.Id, Quantity = 15, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r16.Id, ProductId = tomatoPaste.Id, Quantity = 40, UnitId = uG.Id },

    //        // r17: Куриные котлеты
    //        new RecipeIngredient { RecipeId = r17.Id, ProductId = chicken.Id, Quantity = 500, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r17.Id, ProductId = egg.Id, Quantity = 1, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r17.Id, ProductId = onion.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r17.Id, ProductId = sunflowerOil.Id, Quantity = 30, UnitId = uMl.Id },

    //        // r18: Тушёный кабачок с томатами
    //        new RecipeIngredient { RecipeId = r18.Id, ProductId = zucchini.Id, Quantity = 400, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r18.Id, ProductId = tomato.Id, Quantity = 200, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r18.Id, ProductId = onion.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r18.Id, ProductId = garlic.Id, Quantity = 10, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r18.Id, ProductId = sunflowerOil.Id, Quantity = 20, UnitId = uMl.Id },

    //        // r19: Яичница с помидорами
    //        new RecipeIngredient { RecipeId = r19.Id, ProductId = egg.Id, Quantity = 3, UnitId = uShts.Id },
    //        new RecipeIngredient { RecipeId = r19.Id, ProductId = tomato.Id, Quantity = 150, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r19.Id, ProductId = sunflowerOil.Id, Quantity = 10, UnitId = uMl.Id },

    //        // r20: Говяжий суп с картофелем
    //        new RecipeIngredient { RecipeId = r20.Id, ProductId = beef.Id, Quantity = 400, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r20.Id, ProductId = potato.Id, Quantity = 300, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r20.Id, ProductId = carrot.Id, Quantity = 100, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r20.Id, ProductId = onion.Id, Quantity = 80, UnitId = uG.Id },
    //        new RecipeIngredient { RecipeId = r20.Id, ProductId = garlic.Id, Quantity = 10, UnitId = uG.Id }
    //    );
    //    db.SaveChanges();

    //    // Favorites
    //    db.FavoriteRecipes.AddRange(
    //        new FavoriteRecipe { UserId = admin.Id, RecipeId = r1.Id },
    //        new FavoriteRecipe { UserId = admin.Id, RecipeId = r7.Id },
    //        new FavoriteRecipe { UserId = admin.Id, RecipeId = r4.Id }
    //    );
    //    };

}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API ��� �����������
app.MapPost("/api/auth/login", async ([FromForm] string login, [FromForm] string password, HttpContext ctx, ProductManagerContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Login == login && u.Password == password);
    if (user != null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login ?? "")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return Results.Redirect("/fridge");
    }

    return Results.Redirect("/login?error=invalid");
}).DisableAntiforgery();

app.MapPost("/api/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.Run();

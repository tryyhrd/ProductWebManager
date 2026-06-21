using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductWebManager.Classes.AI;
using ProductWebManager.Data;
using ProductWebManager.Models;

namespace ProductWebManager.Services;

public class MealPlanGeneratorService
{
    private readonly IDbContextFactory<ProductManagerContext> _dbContextFactory;
    private readonly GigaChatHelper _gigaChatAi;
    private readonly RecipeResolverService _recipeResolver;
    private readonly OpenFoodFactsService _openFoodFactsService;
    private readonly ILogger<MealPlanGeneratorService> _logger;

    public MealPlanGeneratorService(
        IDbContextFactory<ProductManagerContext> dbContextFactory,
        GigaChatHelper gigaChatAi,
        RecipeResolverService recipeResolver,
        OpenFoodFactsService openFoodFactsService,
        ILogger<MealPlanGeneratorService> logger)
    {
        _dbContextFactory = dbContextFactory;
        _gigaChatAi = gigaChatAi;
        _recipeResolver = recipeResolver;
        _openFoodFactsService = openFoodFactsService;
        _logger = logger;
    }

    public async Task<bool> GenerateMealPlanAsync(
        int userId,
        UserProfile profile,
        int generationDays,
        GoalType selectedGoal,
        int targetCalories,
        int targetProteins,
        int targetFats,
        int targetCarbs,
        bool useFridgeProducts,
        double minFridgeQuantity,
        IProgress<string>? progress = null)
    {
        progress?.Report("Подготовка данных профиля и холодильника...");
        List<string>? fridgeProductNames = null;
        List<string>? allergies = null;

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            if (useFridgeProducts)
            {
                fridgeProductNames = await dbContext.FridgeItems
                    .Include(f => f.Product)
                    .Where(f => f.UserId == userId && f.Quantity >= minFridgeQuantity)
                    .Select(f => f.Product.Name)
                    .ToListAsync();
            }

            allergies = await dbContext.UserAllergies
                .Include(a => a.Allergy)
                .Where(a => a.UserId == userId)
                .Select(a => a.Allergy.Name)
                .ToListAsync();
        }

        progress?.Report("Запрос к нейросети для формирования меню...");
        var planStructure = await _gigaChatAi.GenerateMealPlanStructureAsync(
            profile,
            generationDays,
            selectedGoal,
            targetCalories,
            targetProteins,
            targetFats,
            targetCarbs,
            useFridgeProducts,
            "balanced",
            null,
            fridgeProductNames,
            allergies);

        if (planStructure == null || planStructure.Days == null || !planStructure.Days.Any())
        {
            throw new Exception("Нейросеть вернула пустой ответ. Попробуйте еще раз.");
        }

        progress?.Report("Обработка рецептов и ингредиентов...");
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var planName = $"План питания от {DateTime.Today:dd.MM.yyyy}";

            var oldPlans = await context.MealPlans.Where(p => p.UserId == userId && p.Name == planName).ToListAsync();
            if (oldPlans.Any())
            {
                context.MealPlans.RemoveRange(oldPlans);
            }

            var newPlan = new Models.MealPlan
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Name = planName,
                Items = new List<MealPlanItem>()
            };

            var categoryCache = new Dictionary<string, Category>();
            var unitCache = new Dictionary<string, Unit>();

            // Загружаем все продукты один раз для нечёткого поиска (вместо ToListAsync на каждый ингредиент)
            var productCache = await context.Products.ToListAsync();

            DateTime startingDate = DateTime.Today;
            for (int dayIndex = 0; dayIndex < generationDays; dayIndex++)
            {
                if (dayIndex >= planStructure.Days.Count) break;
                var currentAiDay = planStructure.Days[dayIndex];
                if (currentAiDay?.Meals == null) continue;

                var targetDate = startingDate.AddDays(dayIndex);
                foreach (var aiMeal in currentAiDay.Meals)
                {
                    var existingRecipe = await _recipeResolver.FindRecipeAsync(aiMeal.Title);
                    Recipe recipeToUse;

                    if (existingRecipe != null)
                    {
                        recipeToUse = await context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefaultAsync(r => r.Id == existingRecipe.Id) ?? existingRecipe;
                    }
                    else
                    {
                        recipeToUse = new Recipe
                        {
                            Title = aiMeal.Title,
                            Description = aiMeal.Description ?? "",
                            Instructions = aiMeal.Instructions != null ? string.Join("\n", aiMeal.Instructions) : "Приготовьте согласно рецепту.",
                            ImageUrl = GetRecipeImageUrl(aiMeal.Title),
                            RecipeIngredients = new List<RecipeIngredient>()
                        };

                        foreach (var aiIng in aiMeal.Ingredients)
                        {
                            var catKey = (aiIng.Category ?? "Разное").Trim().ToLower();
                            if (!categoryCache.TryGetValue(catKey, out var dbCategory))
                            {
                                dbCategory = await context.Set<Category>().FirstOrDefaultAsync(c => c.Name.ToLower() == catKey) ?? new Category { Name = aiIng.Category ?? "Разное" };
                                categoryCache[catKey] = dbCategory;
                            }

                            var unitKey = (aiIng.Unit ?? "г").Trim().ToLower();
                            if (!unitCache.TryGetValue(unitKey, out var dbUnit))
                            {
                                dbUnit = await context.Set<Unit>().FirstOrDefaultAsync(u => u.Name.ToLower() == unitKey) ?? new Unit { Name = aiIng.Unit ?? "г" };
                                unitCache[unitKey] = dbUnit;
                            }

                            var dbProduct = await FindOrCreateProductAsync(context, aiIng, dbCategory, dbUnit, productCache);

                            // Конвертируем количество: если продукт штучный, а AI прислал граммы → переводим в штуки
                            double quantity = (double)aiIng.Quantity;
                            var unitLower = (aiIng.Unit ?? "г").Trim().ToLower();
                            bool aiSentGrams = unitLower is "г" or "гр" or "мл" or "г." or "гр.";
                            if (dbProduct.IsPieceBased && aiSentGrams && dbProduct.AverageWeightGrams > 0)
                            {
                                quantity = Math.Max(1, Math.Round(quantity / dbProduct.AverageWeightGrams.Value, 1));
                            }

                            recipeToUse.RecipeIngredients.Add(new RecipeIngredient { Product = dbProduct, Quantity = quantity, Unit = dbUnit });
                        }
                    }

                    // Всегда пересчитываем КБЖУ из ингредиентов для точности
                    int mealCalories, mealProteins, mealFats, mealCarbs;
                    if (recipeToUse.RecipeIngredients.Any())
                    {
                        mealCalories = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                            NutritionCalculator.CalculateCalories(ri.Product, ri.Quantity)));
                        mealProteins = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                            NutritionCalculator.CalculateProteins(ri.Product, ri.Quantity)));
                        mealFats = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                            NutritionCalculator.CalculateFats(ri.Product, ri.Quantity)));
                        mealCarbs = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                            NutritionCalculator.CalculateCarbs(ri.Product, ri.Quantity)));

                        // Sanity check: если расчёт даёт абсурдные значения, откатываемся на данные от AI
                        if (mealCalories > 5000 || mealCalories <= 0)
                        {
                            mealCalories = (int)(aiMeal.Calories != 0 ? aiMeal.Calories : aiMeal.Ingredients.Sum(i => i.Calories));
                            mealProteins = (int)(aiMeal.Proteins != 0 ? aiMeal.Proteins : aiMeal.Ingredients.Sum(i => i.Proteins));
                            mealFats = (int)(aiMeal.Fats != 0 ? aiMeal.Fats : aiMeal.Ingredients.Sum(i => i.Fats));
                            mealCarbs = (int)(aiMeal.Carbs != 0 ? aiMeal.Carbs : aiMeal.Ingredients.Sum(i => i.Carbs));
                        }
                    }
                    else
                    {
                        mealCalories = (int)(aiMeal.Calories != 0 ? aiMeal.Calories : aiMeal.Ingredients.Sum(i => i.Calories));
                        mealProteins = (int)(aiMeal.Proteins != 0 ? aiMeal.Proteins : aiMeal.Ingredients.Sum(i => i.Proteins));
                        mealFats = (int)(aiMeal.Fats != 0 ? aiMeal.Fats : aiMeal.Ingredients.Sum(i => i.Fats));
                        mealCarbs = (int)(aiMeal.Carbs != 0 ? aiMeal.Carbs : aiMeal.Ingredients.Sum(i => i.Carbs));
                    }

                    var planItem = new Models.MealPlanItem
                    {
                        Date = targetDate,
                        Calories = mealCalories,
                        Proteins = mealProteins,
                        Fats = mealFats,
                        Carbs = mealCarbs,
                        Recipe = recipeToUse,
                        MealType = Enum.TryParse<MealType>(aiMeal.MealType, true, out var parsedEnum) ? parsedEnum : MealType.Snack
                    };
                    newPlan.Items.Add(planItem);
                }
            }

            progress?.Report("Сохранение плана в базу данных...");
            context.MealPlans.Add(newPlan);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to generate meal plan");
            throw;
        }
    }

    public async Task<string> ReplaceMealAsync(
        int userId,
        int mealPlanItemId,
        string currentMealType,
        IEnumerable<string> existingTitles,
        UserProfile profile,
        int targetCalories,
        int targetProteins,
        int targetFats,
        int targetCarbs,
        bool useFridgeProducts,
        double minFridgeQuantity)
    {
        List<string>? fridgeProductNames = null;
        List<string>? allergies = null;

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            if (useFridgeProducts)
            {
                fridgeProductNames = await dbContext.FridgeItems
                    .Include(f => f.Product)
                    .Where(f => f.UserId == userId && f.Quantity >= minFridgeQuantity)
                    .Select(f => f.Product.Name)
                    .ToListAsync();
            }

            allergies = await dbContext.UserAllergies
                .Include(a => a.Allergy)
                .Where(a => a.UserId == userId)
                .Select(a => a.Allergy.Name)
                .ToListAsync();
        }

        // Получаем старое блюдо для определения целевой калорийности замены
        int replacementTargetCalories;
        string originalTitle;
        bool isSnack;
        MealType parsedMealType;

        await using (var dbContext = await _dbContextFactory.CreateDbContextAsync())
        {
            var oldItem = await dbContext.Set<MealPlanItem>()
                .Include(i => i.Recipe)
                .FirstOrDefaultAsync(i => i.Id == mealPlanItemId);

            if (oldItem == null)
                throw new Exception("Элемент рациона не найден в базе данных.");

            // Используем калорийность заменяемого блюда, а не дневную норму
            replacementTargetCalories = oldItem.Calories > 0 ? oldItem.Calories : targetCalories / 4;
            originalTitle = oldItem.Recipe?.Title ?? "Блюдо";
            parsedMealType = oldItem.MealType;
            isSnack = oldItem.MealType == MealType.Snack;
        }

        // Используем специализированный метод для замены одного блюда
        var replacementDto = await _gigaChatAi.FindReplacementRecipeAsync(
            originalTitle,
            parsedMealType,
            replacementTargetCalories,
            isSnack,
            profile,
            profile.Goal,
            existingTitles,
            fridgeProductNames,
            allergies);

        if (replacementDto == null)
        {
            throw new Exception("Нейросеть не смогла сгенерировать замену.");
        }

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var dbItem = await context.Set<MealPlanItem>().Include(i => i.Recipe).FirstOrDefaultAsync(i => i.Id == mealPlanItemId);

            if (dbItem == null)
            {
                throw new Exception("Элемент рациона не найден в базе данных.");
            }

            var existingRecipe = await _recipeResolver.FindRecipeAsync(replacementDto.Title);
            Recipe recipeToUse;

            if (existingRecipe != null)
            {
                recipeToUse = await context.Recipes.Include(r => r.RecipeIngredients).FirstOrDefaultAsync(r => r.Id == existingRecipe.Id) ?? existingRecipe;
            }
            else
            {
                recipeToUse = new Recipe
                {
                    Title = replacementDto.Title,
                    Description = replacementDto.Description ?? "",
                    Instructions = replacementDto.Instructions != null ? string.Join("\n", replacementDto.Instructions) : "Приготовьте по рецепту.",
                    ImageUrl = GetRecipeImageUrl(replacementDto.Title),
                    RecipeIngredients = new List<RecipeIngredient>()
                };

                var categoryCache = new Dictionary<string, Category>();
                var unitCache = new Dictionary<string, Unit>();
                // Загружаем кэш продуктов для нечёткого поиска
                var productCache = await context.Products.ToListAsync();

                foreach (var aiIng in replacementDto.Ingredients)
                {
                    var catKey = (aiIng.Category ?? "Разное").Trim().ToLower();
                    if (!categoryCache.TryGetValue(catKey, out var dbCategory))
                    {
                        dbCategory = await context.Set<Category>().FirstOrDefaultAsync(c => c.Name.ToLower() == catKey) ?? new Category { Name = aiIng.Category ?? "Разное" };
                        categoryCache[catKey] = dbCategory;
                    }

                    var unitKey = (aiIng.Unit ?? "г").Trim().ToLower();
                    if (!unitCache.TryGetValue(unitKey, out var dbUnit))
                    {
                        dbUnit = await context.Set<Unit>().FirstOrDefaultAsync(u => u.Name.ToLower() == unitKey) ?? new Unit { Name = aiIng.Unit ?? "г" };
                        unitCache[unitKey] = dbUnit;
                    }

                    var dbProduct = await FindOrCreateProductAsync(context, aiIng, dbCategory, dbUnit, productCache);

                    // Конвертируем количество: если продукт штучный, а AI прислал граммы → переводим в штуки
                    double quantity = (double)aiIng.Quantity;
                    var unitLower = (aiIng.Unit ?? "г").Trim().ToLower();
                    bool aiSentGrams = unitLower is "г" or "гр" or "мл" or "г." or "гр.";
                    if (dbProduct.IsPieceBased && aiSentGrams && dbProduct.AverageWeightGrams > 0)
                    {
                        quantity = Math.Max(1, Math.Round(quantity / dbProduct.AverageWeightGrams.Value, 1));
                    }

                    recipeToUse.RecipeIngredients.Add(new RecipeIngredient { Product = dbProduct, Quantity = quantity, Unit = dbUnit });

                }
            }

            // Всегда пересчитываем КБЖУ из ингредиентов рецепта для точности
            // (не доверяем AI-значениям, которые могут быть шаблонными 20/15/50)
            int mealCalories, mealProteins, mealFats, mealCarbs;
            if (recipeToUse.RecipeIngredients.Any())
            {
                mealCalories = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                    NutritionCalculator.CalculateCalories(ri.Product, ri.Quantity)));
                mealProteins = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                    NutritionCalculator.CalculateProteins(ri.Product, ri.Quantity)));
                mealFats = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                    NutritionCalculator.CalculateFats(ri.Product, ri.Quantity)));
                mealCarbs = (int)Math.Round(recipeToUse.RecipeIngredients.Sum(ri =>
                    NutritionCalculator.CalculateCarbs(ri.Product, ri.Quantity)));

                // Sanity check: если расчёт даёт абсурдные значения (>5000 ккал на блюдо),
                // откатываемся на данные от AI
                if (mealCalories > 5000 || mealCalories <= 0)
                {
                    mealCalories = replacementDto.Calories > 0 ? replacementDto.Calories : replacementTargetCalories;
                    mealProteins = replacementDto.Proteins;
                    mealFats = replacementDto.Fats;
                    mealCarbs = replacementDto.Carbs;
                }
            }
            else
            {
                mealCalories = replacementDto.Calories > 0 ? replacementDto.Calories : replacementTargetCalories;
                mealProteins = replacementDto.Proteins;
                mealFats = replacementDto.Fats;
                mealCarbs = replacementDto.Carbs;
            }

            dbItem.Calories = mealCalories;
            dbItem.Proteins = mealProteins;
            dbItem.Fats = mealFats;
            dbItem.Carbs = mealCarbs;
            dbItem.Recipe = recipeToUse;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            return replacementDto.Title;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to replace meal");
            throw;
        }
    }

    /// <summary>
    /// Возвращает URL изображения для рецепта на основе Unsplash по ключевым словам в названии.
    /// Используется для AI-сгенерированных рецептов, у которых нет фото.
    /// </summary>
    private static string GetRecipeImageUrl(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "";
        var t = title.ToLowerInvariant();

        // Завтраки / каши
        if (t.Contains("овсян") || t.Contains("каша") || t.Contains("мюсли") || t.Contains("гранола"))
            return "https://images.unsplash.com/photo-1517673400267-0251440c45dc?w=800";
        if (t.Contains("омлет") || t.Contains("яичниц") || t.Contains("яйц"))
            return "https://images.unsplash.com/photo-1510693206972-df098062cb71?w=800";
        if (t.Contains("блин") || t.Contains("панкейк"))
            return "https://images.unsplash.com/photo-1528207776546-365bb710ee93?w=800";
        if (t.Contains("творог") || t.Contains("запеканк"))
            return "https://images.unsplash.com/photo-1602351447937-745cb720612f?w=800";

        // Мясо / птица
        if (t.Contains("курин") || t.Contains("куриц") || t.Contains("курица"))
            return "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800";
        if (t.Contains("говяд") || t.Contains("стейк") || t.Contains("мясо"))
            return "https://images.unsplash.com/photo-1546833998-877b37c2e5c6?w=800";
        if (t.Contains("свинин") || t.Contains("котлет"))
            return "https://images.unsplash.com/photo-1614777735417-4959b46e9799?w=800";
        if (t.Contains("индейк"))
            return "https://images.unsplash.com/photo-1574672280600-4accfa5b6f98?w=800";

        // Рыба / морепродукты
        if (t.Contains("лосос") || t.Contains("семга"))
            return "https://images.unsplash.com/photo-1519708227418-c8fd9a32b7a2?w=800";
        if (t.Contains("рыб") || t.Contains("треска") || t.Contains("тунец"))
            return "https://images.unsplash.com/photo-1562802378-063ec186a863?w=800";
        if (t.Contains("креветк"))
            return "https://images.unsplash.com/photo-1565680018434-b513d5e5fd47?w=800";

        // Крупы / гарниры
        if (t.Contains("гречк") || t.Contains("гречнев"))
            return "https://images.unsplash.com/photo-1586201375761-83865001e31c?w=800";
        if (t.Contains("рис"))
            return "https://images.unsplash.com/photo-1516684732162-798a0062be99?w=800";
        if (t.Contains("макарон") || t.Contains("паста") || t.Contains("спагетт"))
            return "https://images.unsplash.com/photo-1621996346565-e3dbc646d9a9?w=800";

        // Супы
        if (t.Contains("борщ"))
            return "https://images.unsplash.com/photo-1548943487-a2e4e43b4853?w=800";
        if (t.Contains("суп") || t.Contains("бульон"))
            return "https://images.unsplash.com/photo-1547592166-23ac45744acd?w=800";
        if (t.Contains("крем-суп") || t.Contains("брокол"))
            return "https://images.unsplash.com/photo-1543826173-1beeb97525d8?w=800";

        // Салаты
        if (t.Contains("салат") || t.Contains("греческ"))
            return "https://images.unsplash.com/photo-1540189549336-e6e99c3679fe?w=800";

        // Десерты / перекусы
        if (t.Contains("банан") || t.Contains("фрукт"))
            return "https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=800";
        if (t.Contains("йогурт"))
            return "https://images.unsplash.com/photo-1571019613454-1cb2f99b2d8b?w=800";
        if (t.Contains("орех") || t.Contains("миндал"))
            return "https://images.unsplash.com/photo-1574570068036-d3b60d4b4a47?w=800";
        if (t.Contains("шоколад") || t.Contains("какао"))
            return "https://images.unsplash.com/photo-1606312619070-d48b6b6f1e4a?w=800";

        // Овощные блюда
        if (t.Contains("овощ") || t.Contains("рататуй") || t.Contains("тушен"))
            return "https://images.unsplash.com/photo-1572453800999-e8d2d1589b7c?w=800";

        // Дефолтное изображение еды
        return "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=800";
    }

    private async Task<Product> FindOrCreateProductAsync(
        ProductWebManager.Data.ProductManagerContext context,
        GigaChatHelper.GeneratedIngredientDto aiIng,
        Category dbCategory,
        Unit dbUnit,
        List<Product>? productCache = null)
    {
        var normalizedName = ProductNameNormalizer.Normalize(aiIng.Name);

        var dbProduct = await context.Products.FirstOrDefaultAsync(
            p => p.Name.ToLower() == normalizedName);

        if (dbProduct == null)
        {
            // Используем кэш вместо повторной загрузки всех продуктов из БД
            var searchList = productCache ?? await context.Products.ToListAsync();
            dbProduct = searchList.FirstOrDefault(
                p => ProductNameNormalizer.AreSimilar(p.Name, aiIng.Name));
        }

        if (dbProduct != null)
        {
            // Обновляем КБЖУ продукта, если в БД они нулевые, а AI прислал данные
            if (dbProduct.Calories == 0 && aiIng.Calories > 0)
            {
                dbProduct.Calories = aiIng.Calories;
                dbProduct.Proteins = aiIng.Proteins;
                dbProduct.Fats = aiIng.Fats;
                dbProduct.Carbohydrates = aiIng.Carbs;
            }
            return dbProduct;
        }

        double apiCalories = aiIng.Calories;
        double apiProteins = aiIng.Proteins;
        double apiFats = aiIng.Fats;
        double apiCarbs = aiIng.Carbs;
        string? imageUrl = null;

        try
        {
            var apiResults = await _openFoodFactsService.SearchProductsAsync(aiIng.Name);
            var bestMatch = apiResults.FirstOrDefault(p => p.Nutriments != null && p.Nutriments.Calories100g.HasValue);
            
            if (bestMatch?.Nutriments != null)
            {
                apiCalories = bestMatch.Nutriments.Calories100g ?? apiCalories;
                apiProteins = bestMatch.Nutriments.Proteins100g ?? apiProteins;
                apiFats = bestMatch.Nutriments.Fat100g ?? apiFats;
                apiCarbs = bestMatch.Nutriments.Carbohydrates100g ?? apiCarbs;
                imageUrl = bestMatch.ImageUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Failed to fetch API data for {aiIng.Name}");
        }

        dbProduct = new Product
        {
            Name = aiIng.Name.Trim(),
            Category = dbCategory,
            Unit = dbUnit,
            Proteins = apiProteins,
            Fats = apiFats,
            Carbohydrates = apiCarbs,
            Calories = apiCalories,
            ImageUrl = imageUrl,
            IsPieceBased = false
        };
        context.Products.Add(dbProduct);

        // Добавляем в кэш, чтобы следующие ингредиенты нашли этот продукт
        productCache?.Add(dbProduct);

        return dbProduct;
    }
}

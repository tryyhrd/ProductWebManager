using ProductWebManager.Models;

namespace ProductWebManager.Services;

public static class RecipeNutritionAdjuster
{
    private const double Tolerance = 0.03;

    private const double MinScale = 0.5;
    private const double MaxScale = 2.0;
    private const double FineTuneMaxAdjust = 0.2;
    private const double MinSignificantCalories = 15;
    private const double MinSignificantGrams = 10;
    private const int MaxPieceItems = 8;
    private const int MaxGrams = 500;

    public static MealNutrition CalculateNutrition(Recipe recipe)
    {
        double calories = 0, proteins = 0, fats = 0, carbs = 0;

        foreach (var ingredient in recipe.RecipeIngredients ?? new List<RecipeIngredient>())
        {
            if (ingredient.Product == null) continue;

            calories += NutritionCalculator.CalculateCalories(ingredient.Product, ingredient.Quantity, ingredient.Unit);
            proteins += NutritionCalculator.CalculateProteins(ingredient.Product, ingredient.Quantity, ingredient.Unit);
            fats += NutritionCalculator.CalculateFats(ingredient.Product, ingredient.Quantity, ingredient.Unit);
            carbs += NutritionCalculator.CalculateCarbs(ingredient.Product, ingredient.Quantity, ingredient.Unit);
        }

        return new MealNutrition(calories, proteins, fats, carbs);
    }

    public static MealNutrition BuildTargetNutrition(int targetCalories)
    {
        return new MealNutrition(
            targetCalories,
            targetCalories * 0.30 / 4.0,
            targetCalories * 0.25 / 9.0,
            targetCalories * 0.45 / 4.0);
    }

    public static bool IsWithinTolerance(MealNutrition target, MealNutrition actual)
    {
        if (target.Calories <= 0) return true;
        return Math.Abs(actual.Calories - target.Calories) / target.Calories <= Tolerance;
    }

    public static MealNutrition ScaleRecipeToTarget(
        Recipe recipe,
        int targetCalories,
        double minScale = MinScale,
        double maxScale = MaxScale)
    {
        var current = CalculateNutrition(recipe);
        if (current.Calories <= 0) return current;

        double factor = targetCalories / current.Calories;
        factor = Math.Clamp(factor, minScale, maxScale);

        if (Math.Abs(factor - 1.0) < 0.02) return current;

        foreach (var ingredient in recipe.RecipeIngredients)
        {
            if (ingredient.Product == null) continue;

            double ingredientCalories = NutritionCalculator.CalculateCalories(
                ingredient.Product, ingredient.Quantity, ingredient.Unit);

            bool isMinor = ingredientCalories < MinSignificantCalories &&
                           ingredient.Quantity < MinSignificantGrams;

            if (isMinor) continue;

            double newQuantity = ingredient.Quantity * factor;
            double maxQuantity = ingredient.Product.IsPieceBased ? MaxPieceItems : MaxGrams;

            ingredient.Quantity = Math.Round(Math.Clamp(newQuantity, 0.1, maxQuantity), 1);
        }

        return CalculateNutrition(recipe);
    }

    public static MealNutrition FineTuneCalories(Recipe recipe, int targetCalories)
    {
        var current = CalculateNutrition(recipe);
        double delta = targetCalories - current.Calories;

        if (targetCalories > 0 && Math.Abs(delta) < targetCalories * Tolerance)
            return current;

        var mainIngredient = recipe.RecipeIngredients
            .Where(i => i.Product != null)
            .Where(i =>
            {
                double cals = NutritionCalculator.CalculateCalories(i.Product!, i.Quantity, i.Unit);
                return cals >= MinSignificantCalories || i.Quantity >= MinSignificantGrams;
            })
            .OrderByDescending(i => NutritionCalculator.CalculateCalories(i.Product!, i.Quantity, i.Unit))
            .FirstOrDefault();

        if (mainIngredient?.Product == null) return current;

        double caloriesPerUnit = NutritionCalculator.CalculateCalories(mainIngredient.Product, 1, mainIngredient.Unit);
        if (caloriesPerUnit <= 0) return current;

        double adjustQuantity = delta / caloriesPerUnit;
        double maxAdjust = mainIngredient.Quantity * FineTuneMaxAdjust;
        adjustQuantity = Math.Clamp(adjustQuantity, -maxAdjust, maxAdjust);

        double newQuantity = mainIngredient.Quantity + adjustQuantity;
        double maxQuantity = mainIngredient.Product.IsPieceBased ? MaxPieceItems : MaxGrams;

        mainIngredient.Quantity = Math.Round(Math.Clamp(newQuantity, 0.1, maxQuantity), 1);

        return CalculateNutrition(recipe);
    }

    // В RecipeNutritionAdjuster.cs
    public static MealNutrition SmartNormalize(Recipe recipe, int targetCalories)
    {
        var current = CalculateNutrition(recipe);
        double diff = targetCalories - current.Calories;
        if (Math.Abs(diff) < targetCalories * 0.03) 
            return current;

        var candidates = recipe.RecipeIngredients
            .Where(i => i.Product != null)
            .Where(i => i.Product!.Carbohydrates > 30 || i.Product!.Fats > 30)
            .OrderByDescending(i => NutritionCalculator.CalculateCalories(i.Product!, i.Quantity, i.Unit))
            .ToList();

        if (!candidates.Any())
            candidates = recipe.RecipeIngredients
                .Where(i => i.Product != null)
                .OrderByDescending(i => NutritionCalculator.CalculateCalories(i.Product!, i.Quantity, i.Unit))
                .ToList();

        var target = candidates.FirstOrDefault();
        if (target?.Product == null) return current;

        double calPerUnit = NutritionCalculator.CalculateCalories(target.Product, 1, target.Unit);
        if (calPerUnit <= 0) return current;

        double adjust = diff / calPerUnit;
        double maxAdjust = target.Quantity * 0.3; // Не более 30% изменения
        adjust = Math.Clamp(adjust, -maxAdjust, maxAdjust);

        double newQty = target.Quantity + adjust;
        double maxAllowed = target.Product.IsPieceBased ? 8 : 500;
        target.Quantity = Math.Round(Math.Clamp(newQty, 1, maxAllowed), 1);

        return CalculateNutrition(recipe);
    }

    public static MealNutrition AdjustRecipeToTarget(
        Recipe recipe,
        int targetCalories,
        double minScale = MinScale,
        double maxScale = MaxScale)
    {
        var nutrition = ScaleRecipeToTarget(recipe, targetCalories, minScale, maxScale);

        if (targetCalories > 0 && Math.Abs(nutrition.Calories - targetCalories) > targetCalories * Tolerance)
        {
            nutrition = FineTuneCalories(recipe, targetCalories);
        }

        return nutrition;
    }

    /// <summary>
    /// Умная балансировка: корректирует пропорции, а не только масштаб
    /// </summary>
    public static MealNutrition SmartBalanceRecipe(Recipe recipe, int targetCalories)
    {
        var current = CalculateNutrition(recipe);
        if (current.Calories <= 0) return current;

        // 1. Группируем ингредиенты по типу
        var ingredients = recipe.RecipeIngredients
            .Where(i => i.Product != null)
            .ToList();

        var mains = ingredients.Where(i => IsMainIngredient(i.Product!)).ToList();
        var liquids = ingredients.Where(i => IsLiquidIngredient(i.Product!)).ToList();
        var extras = ingredients.Where(i => !mains.Contains(i) && !liquids.Contains(i)).ToList();

        // 2. Рассчитываем целевые доли (пример: 50% основы, 30% жидкости, 20% добавки)
        double targetMainCalories = targetCalories * 0.50;
        double targetLiquidCalories = targetCalories * 0.30;
        double targetExtraCalories = targetCalories * 0.20;

        // 3. Корректируем основные ингредиенты
        if (mains.Any())
        {
            double currentMainCals = mains.Sum(i => NutritionCalculator.CalculateCalories(i.Product!, i.Quantity, i.Unit));
            double mainFactor = currentMainCals > 0 ? targetMainCalories / currentMainCals : 1.0;
            mainFactor = Math.Clamp(mainFactor, 0.5, 2.0);

            foreach (var ing in mains)
            {
                double newQty = ing.Quantity * mainFactor;
                double maxQty = ing.Product!.IsPieceBased ? MaxPieceItems : MaxGrams;
                ing.Quantity = Math.Round(Math.Clamp(newQty, 10, maxQty), 1);
            }
        }

        // 4. Корректируем жидкости (ограничиваем разумными пределами)
        foreach (var ing in liquids)
        {
            double calPerUnit = NutritionCalculator.CalculateCalories(ing.Product!, 1);
            if (calPerUnit <= 0) continue;

            // Жидкости: не более 250-300 мл для напитков, 100-150 для соусов
            double maxLiquidQty = ing.Product!.Name.ToLower().Contains("молоко") ||
                                  ing.Product!.Name.ToLower().Contains("кефир") ? 250 : 100;

            double targetQty = Math.Min(maxLiquidQty,
                (targetLiquidCalories / liquids.Count) / calPerUnit);

            ing.Quantity = Math.Round(Math.Clamp(targetQty, 30, maxLiquidQty), 1);
        }

        // 5. Оставшиеся калории распределяем на добавки
        double usedCalories = CalculateNutrition(recipe).Calories;
        double remainingCalories = targetCalories - usedCalories;

        if (Math.Abs(remainingCalories) > 10 && extras.Any()) // если отклонение >10 ккал
        {
            var biggestExtra = extras.OrderByDescending(e =>
                NutritionCalculator.CalculateCalories(e.Product!, e.Quantity, e.Unit)).First();

            double calPerUnit = NutritionCalculator.CalculateCalories(biggestExtra.Product!, 1, biggestExtra.Unit);
            if (calPerUnit > 0)
            {
                double adjustQty = remainingCalories / calPerUnit;
                double newQty = biggestExtra.Quantity + adjustQty;
                double maxQty = biggestExtra.Product!.IsPieceBased ? MaxPieceItems : MaxGrams;
                biggestExtra.Quantity = Math.Round(Math.Clamp(newQty, 5, maxQty), 1);
            }
        }

        return CalculateNutrition(recipe);
    }

    private static bool IsMainIngredient(Product p)
    {
        var name = p.Name.ToLower();
        return name.Contains("греч") || name.Contains("рис") || name.Contains("макарон") ||
               name.Contains("куриц") || name.Contains("говядин") || name.Contains("рыб") ||
               name.Contains("яйц") || name.Contains("творог") || name.Contains("овсян");
    }

    private static bool IsLiquidIngredient(Product p)
    {
        var name = p.Name.ToLower();
        var unit = p.Unit?.Name?.ToLower() ?? "";
        return name.Contains("молоко") || name.Contains("кефир") || name.Contains("йогурт") ||
               name.Contains("вода") || name.Contains("сок") || name.Contains("масло") ||
               unit == "мл" || unit == "л";
    }

    public static MealNutrition NormalizeRecipe(Recipe recipe, int targetCalories)
    {
        return AdjustRecipeToTarget(recipe, targetCalories);
    }
}
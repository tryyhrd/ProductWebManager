using ProductWebManager.Models;

namespace ProductWebManager.Services;

public static class RecipeNutritionAdjuster
{
    // 🔥 КЛЮЧЕВОЕ ИЗМЕНЕНИЕ: 3% допуск для рецептов, чтобы день укладывался в 5%
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

            calories += NutritionCalculator.CalculateCalories(ingredient.Product, ingredient.Quantity);
            proteins += NutritionCalculator.CalculateProteins(ingredient.Product, ingredient.Quantity);
            fats += NutritionCalculator.CalculateFats(ingredient.Product, ingredient.Quantity);
            carbs += NutritionCalculator.CalculateCarbs(ingredient.Product, ingredient.Quantity);
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
                ingredient.Product, ingredient.Quantity);

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
                double cals = NutritionCalculator.CalculateCalories(i.Product!, i.Quantity);
                return cals >= MinSignificantCalories || i.Quantity >= MinSignificantGrams;
            })
            .OrderByDescending(i => NutritionCalculator.CalculateCalories(i.Product!, i.Quantity))
            .FirstOrDefault();

        if (mainIngredient?.Product == null) return current;

        double caloriesPerUnit = NutritionCalculator.CalculateCalories(mainIngredient.Product, 1);
        if (caloriesPerUnit <= 0) return current;

        double adjustQuantity = delta / caloriesPerUnit;
        double maxAdjust = mainIngredient.Quantity * FineTuneMaxAdjust;
        adjustQuantity = Math.Clamp(adjustQuantity, -maxAdjust, maxAdjust);

        double newQuantity = mainIngredient.Quantity + adjustQuantity;
        double maxQuantity = mainIngredient.Product.IsPieceBased ? MaxPieceItems : MaxGrams;

        mainIngredient.Quantity = Math.Round(Math.Clamp(newQuantity, 0.1, maxQuantity), 1);

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

    public static MealNutrition NormalizeRecipe(Recipe recipe, int targetCalories)
    {
        return AdjustRecipeToTarget(recipe, targetCalories);
    }
}
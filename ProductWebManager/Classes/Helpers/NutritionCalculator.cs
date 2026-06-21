using ProductWebManager.Models;

namespace ProductWebManager.Services;

public static class NutritionCalculator
{
    public static double CalculateCalories(
        Product? product,
        double quantity,
        Unit? unit = null)
    {
        return Calculate(product, quantity, x => x.Calories, unit);
    }

    public static double CalculateProteins(
        Product? product,
        double quantity,
        Unit? unit = null)
    {
        return Calculate(product, quantity, x => x.Proteins, unit);
    }

    public static double CalculateFats(
        Product? product,
        double quantity,
        Unit? unit = null)
    {
        return Calculate(product, quantity, x => x.Fats, unit);
    }

    public static double CalculateCarbs(
        Product? product,
        double quantity,
        Unit? unit = null)
    {
        return Calculate(product, quantity, x => x.Carbohydrates, unit);
    }

    private static double Calculate(
        Product? product,
        double quantity,
        Func<Product, double> selector,
        Unit? unit = null)
    {
        if (product == null)
            return 0;

        double grams;
        var unitName = unit?.Name?.ToLower() ?? "г";
        bool isMassUnit = unitName is "г" or "гр" or "мл" or "г." or "гр." or "грамм" or "граммов" or "л" or "литр" or "литров";
        bool isPieceUnit = unitName is "шт" or "шт." or "штука" or "штук" or "порция" or "порций";

        if (product.IsPieceBased && !isMassUnit)
        {
            grams = quantity * (product.AverageWeightGrams ?? 100);
        }
        else if (isPieceUnit)
        {
            grams = quantity * (product.AverageWeightGrams ?? 100);
        }
        else
        {
            grams = quantity;
        }

        return selector(product) * grams / 100.0;
    }
}
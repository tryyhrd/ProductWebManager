using ProductWebManager.Models;

namespace ProductWebManager.Services;

public static class NutritionCalculator
{
    public static double CalculateCalories(
        Product? product,
        double quantity)
    {
        return Calculate(product, quantity, x => x.Calories);
    }

    public static double CalculateProteins(
        Product? product,
        double quantity)
    {
        return Calculate(product, quantity, x => x.Proteins);
    }

    public static double CalculateFats(
        Product? product,
        double quantity)
    {
        return Calculate(product, quantity, x => x.Fats);
    }

    public static double CalculateCarbs(
        Product? product,
        double quantity)
    {
        return Calculate(product, quantity, x => x.Carbohydrates);
    }

    private static double Calculate(
        Product? product,
        double quantity,
        Func<Product, double> selector)
    {
        if (product == null)
            return 0;

        double grams;

        if (product.IsPieceBased)
        {
            grams =
                quantity *
                (product.AverageWeightGrams ?? 100);
        }
        else
        {
            grams = quantity;
        }

        return selector(product) * grams / 100.0;
    }
}
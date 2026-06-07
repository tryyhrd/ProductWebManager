using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ProductWebManager.Data;
using ProductWebManager.Models;

var options = new DbContextOptionsBuilder<ProductManagerContext>()
    .UseSqlite("Data Source=C:\\Projects\\ProductWebManager\\ProductWebManager\\app.db")
    .Options;

using var db = new ProductManagerContext(options);
var plan = db.MealPlans.Include(x => x.Items).ThenInclude(x => x.Recipe).ThenInclude(x => x.RecipeIngredients).ThenInclude(x => x.Product).FirstOrDefault();
if (plan != null)
{
    Console.WriteLine($"Plan: {plan.Name}");
    foreach (var item in plan.Items)
    {
        Console.WriteLine($"Meal: {item.MealType} ({item.Calories} kcal, B:{item.Proteins} F:{item.Fats} C:{item.Carbs})");
        if (item.Recipe != null)
        {
            double cals = 0;
            foreach(var ing in item.Recipe.RecipeIngredients)
            {
                if (ing.Product != null)
                {
                    double quantity = ing.Quantity == 0 ? 100 : ing.Quantity;
                    cals += ing.Product.Calories * quantity / 100.0;
                    Console.WriteLine($"  - {ing.Product.Name}: {quantity}g ({ing.Product.Calories} cal/100g) -> {ing.Product.Calories * quantity / 100.0} kcal");
                }
            }
            Console.WriteLine($"  Calculated cals: {cals}");
        }
    }
}
else
{
    Console.WriteLine("No meal plans found.");
}

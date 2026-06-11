using System;
using System.Collections.Generic;

namespace ProductWebManager.Models
{
    public class MealPlanDay
    {
        public DateTime Date { get; set; }
        public bool IsExpanded { get; set; }
        public bool IncludeInShoppingList { get; set; } = true;
        public List<MealPlanMeal> Meals { get; set; } = new();
    }

    public class MealPlanMeal
    {
        public int MealPlanItemId { get; set; }
        public string Title { get; set; } = "";
        public string MealType { get; set; } = "";
        public int Calories { get; set; }
        public int Proteins { get; set; }
        public int Fats { get; set; }
        public int Carbs { get; set; }
        public List<MealIngredientDto> Ingredients { get; set; } = new();
        public Recipe? Recipe { get; set; }
    }

    public class MealIngredientDto
    {
        public string Name { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "";
        public int Calories { get; set; }
        public int Proteins { get; set; }
        public int Fats { get; set; }
        public int Carbs { get; set; }
        public string? ImageUrl { get; set; }
        public double FridgeQuantity { get; set; }
    }

    public class ShoppingItemDto
    {
        public string Name { get; set; } = "";
        public double Quantity { get; set; }
        public string Unit { get; set; } = "";
        public bool IsChecked { get; set; }
    }
}

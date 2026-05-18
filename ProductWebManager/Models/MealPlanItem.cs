namespace ProductWebManager.Models
{
    public class MealPlanItem
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }
        public int RecipeId { get; set; }
        public int Calories { get; set; }
        public int Proteins { get; set; }
        public int Fats { get; set; }
        public int Carbs { get; set; }

        public DateTime Date { get; set; }
        public MealType MealType { get; set; }
        public MealPlan MealPlan { get; set; }
        public Recipe Recipe { get; set; }
    }

    public enum MealType
    {
        Breakfast,
        Lunch,
        Dinner,
        Snack
    }
}

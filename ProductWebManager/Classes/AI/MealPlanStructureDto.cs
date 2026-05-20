using static ProductWebManager.Classes.AI.GigaChatHelper;

namespace ProductWebManager.Classes.AI;

public class MealPlanStructureDto
{
    public string Name { get; set; } = "";
    public List<MealPlanStructureDayDto> Days { get; set; } = new();
}

public class MealPlanStructureDayDto
{
    public int DayNumber { get; set; }
    public List<MealStructureDto> Meals { get; set; } = new();
}

public class MealStructureDto
{
    public string MealType { get; set; } = "";
    public string Title { get; set; } = "";
    public bool IsSnack { get; set; }

    public int TargetCalories { get; set; }
    public int Calories { get; set; }
    public int Proteins { get; set; }
    public int Fats { get; set; }
    public int Carbs { get; set; }
    public string Description { get; set; } = "";
    public List<string> Instructions { get; set; } = [];
    public List<GeneratedIngredientDto> Ingredients { get; set; } = [];
}
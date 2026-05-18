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
}
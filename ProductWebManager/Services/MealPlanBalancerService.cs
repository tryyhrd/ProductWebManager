using ProductWebManager.Classes.AI;

namespace ProductWebManager.Services;

public class MealPlanBalancerService
{
    private const int MinMealCalories = 50;

    public List<MealStructureDto> DistributeCalories(List<MealStructureDto> meals, int targetCalories)
    {
        if (meals == null || meals.Count == 0)
            return new List<MealStructureDto>();

        var result = meals.ToList();

        foreach (var m in result)
        {
            m.MealType = NormalizeMealType(m.MealType);
        }

        var shares = result.Select(GetShare).ToList();

        var allocated = new int[result.Count];
        for (int i = 0; i < result.Count; i++)
            allocated[i] = (int)Math.Round(targetCalories * shares[i]);

        var diff = targetCalories - allocated.Sum();
        var order = GetAdjustmentOrder(result, diff > 0);

        if (order.Count == 0)
            order = Enumerable.Range(0, result.Count).ToList();

        var guard = 0;

        while (diff != 0 && guard < 10000)
        {
            var changed = false;

            foreach (var index in order)
            {
                if (diff == 0)
                    break;

                if (diff > 0)
                {
                    allocated[index]++;
                    diff--;
                    changed = true;
                }
                else
                {
                    if (allocated[index] > MinMealCalories)
                    {
                        allocated[index]--;
                        diff++;
                        changed = true;
                    }
                }
            }

            if (!changed)
                break;

            guard++;
        }

        for (int i = 0; i < result.Count; i++)
            result[i].TargetCalories = Math.Max(0, allocated[i]);

        return result;
    }

    private static double GetShare(MealStructureDto meal)
    {
        return NormalizeMealType(meal.MealType) switch
        {
            "Breakfast" => 0.25,
            "Lunch" => 0.35,
            "Dinner" => 0.30,
            "Snack" => 0.10,
            _ => meal.IsSnack ? 0.10 : 0.25
        };
    }

    private static List<int> GetAdjustmentOrder(List<MealStructureDto> meals, bool increase)
    {
        var priority = increase
            ? new[] { "Lunch", "Dinner", "Breakfast", "Snack" }
            : new[] { "Snack", "Dinner", "Breakfast", "Lunch" };

        var indices = new List<int>();

        foreach (var type in priority)
        {
            for (int i = 0; i < meals.Count; i++)
            {
                if (NormalizeMealType(meals[i].MealType) == type)
                    indices.Add(i);
            }
        }

        if (indices.Count == 0)
            indices = Enumerable.Range(0, meals.Count).ToList();

        return indices;
    }

    private static string NormalizeMealType(string? value)
    {
        var v = (value ?? string.Empty).Trim().ToLowerInvariant();

        return v switch
        {
            "breakfast" or "завтрак" => "Breakfast",
            "lunch" or "обед" => "Lunch",
            "dinner" or "ужин" => "Dinner",
            "snack" or "перекус" => "Snack",
            _ => "Snack"
        };
    }
}
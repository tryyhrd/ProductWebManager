using ProductWebManager.Models;

namespace ProductWebManager.Services
{
    public record NutritionTargets(int Calories, int Proteins, int Fats, int Carbs);

    public static class NutritionService
    {
        public static NutritionTargets CalculateTargets(UserProfile profile)
        {
            double bmr = (profile.Gender == Gender.Male)
                ? (10 * profile.Weight) + (6.25 * profile.Height) - (5 * profile.Age) + 5
                : (10 * profile.Weight) + (6.25 * profile.Height) - (5 * profile.Age) - 161;

            double activityMultiplier = profile.ActivityLevel switch
            {
                ActivityLevel.Low => 1.2,
                ActivityLevel.Medium => 1.55,
                ActivityLevel.High => 1.8,
                _ => 1.2
            };

            double calories = bmr * activityMultiplier;

            calories = profile.Goal switch
            {
                GoalType.LoseWeight => calories - 300,
                GoalType.GainWeight => calories + 300,
                _ => calories
            };

            return new NutritionTargets(
                (int)Math.Round(calories),
                (int)Math.Round((calories * 0.30) / 4),
                (int)Math.Round((calories * 0.25) / 9),
                (int)Math.Round((calories * 0.45) / 4)
            );
        }
    }
}

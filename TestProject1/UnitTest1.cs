using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using ProductWebManager.Models;
using ProductWebManager.Services;
using ProductWebManager.Classes.AI;

namespace TestProject1
{
    // =============================================
    // Тесты модуля холодильника
    // =============================================
    public class FridgeModuleTests
    {
        [Fact]
        public void Test_ExpiryDateCalculation()
        {
            var item = new FridgeItem { ExpirationDate = DateTime.Now, Quantity = 1 };
            var daysLeft = (item.ExpirationDate.Value - DateTime.Now).TotalDays;
            var statusText = daysLeft <= 0 ? "Истек" : $"Истекает через {Math.Ceiling(daysLeft)} дн.";
            Assert.Equal("Истек", statusText);
        }

        [Fact]
        public void Test_StatsCalculation()
        {
            var products = new List<FridgeItem>
            {
                new FridgeItem { ExpirationDate = DateTime.Now },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(1) },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(5) }
            };
            var expiringToday = products.Count(p => p.ExpirationDate.HasValue
                && p.ExpirationDate.Value.Date == DateTime.Now.Date);
            Assert.Equal(1, expiringToday);
        }

        [Fact]
        public void Test_ExpiredProductsCount()
        {
            var products = new List<FridgeItem>
            {
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(-2) },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(-1) },
                new FridgeItem { ExpirationDate = DateTime.Now.AddDays(5) },
                new FridgeItem { ExpirationDate = null }
            };
            var expired = products.Count(p =>
                p.ExpirationDate.HasValue && p.ExpirationDate.Value.Date < DateTime.Today);
            Assert.Equal(2, expired);
        }

        [Fact]
        public void Test_ExpiringWithin3Days()
        {
            var products = new List<FridgeItem>
            {
                new FridgeItem { ExpirationDate = DateTime.Today },
                new FridgeItem { ExpirationDate = DateTime.Today.AddDays(1) },
                new FridgeItem { ExpirationDate = DateTime.Today.AddDays(3) },
                new FridgeItem { ExpirationDate = DateTime.Today.AddDays(5) }
            };
            var expiringSoon = products.Count(p =>
                p.ExpirationDate.HasValue
                && (p.ExpirationDate.Value.Date - DateTime.Today).Days >= 0
                && (p.ExpirationDate.Value.Date - DateTime.Today).Days <= 3);
            Assert.Equal(3, expiringSoon);
        }
    }

    // =============================================
    // Тесты NutritionCalculator (КБЖУ на 100г)
    // =============================================
    public class NutritionCalculatorTests
    {
        [Fact]
        public void Calculate_WeightBasedProduct_100g_ReturnsExactValues()
        {
            // Продукт: КБЖУ на 100г
            var product = new Product
            {
                Calories = 180, Proteins = 23, Fats = 8, Carbohydrates = 0,
                IsPieceBased = false
            };
            // 100г продукта должно дать ровно заявленные значения
            Assert.Equal(180, NutritionCalculator.CalculateCalories(product, 100));
            Assert.Equal(23, NutritionCalculator.CalculateProteins(product, 100));
            Assert.Equal(8, NutritionCalculator.CalculateFats(product, 100));
            Assert.Equal(0, NutritionCalculator.CalculateCarbs(product, 100));
        }

        [Fact]
        public void Calculate_WeightBasedProduct_200g_ReturnsDoubledValues()
        {
            var product = new Product
            {
                Calories = 330, Proteins = 7, Fats = 1, Carbohydrates = 74,
                IsPieceBased = false
            };
            // 200г = 2x от 100г
            Assert.Equal(660, NutritionCalculator.CalculateCalories(product, 200));
            Assert.Equal(14, NutritionCalculator.CalculateProteins(product, 200));
            Assert.Equal(2, NutritionCalculator.CalculateFats(product, 200));
            Assert.Equal(148, NutritionCalculator.CalculateCarbs(product, 200));
        }

        [Fact]
        public void Calculate_WeightBasedProduct_50g_ReturnsHalfValues()
        {
            var product = new Product
            {
                Calories = 200, Proteins = 10, Fats = 6, Carbohydrates = 30,
                IsPieceBased = false
            };
            // 50г = 0.5x от 100г
            Assert.Equal(100, NutritionCalculator.CalculateCalories(product, 50));
            Assert.Equal(5, NutritionCalculator.CalculateProteins(product, 50));
            Assert.Equal(3, NutritionCalculator.CalculateFats(product, 50));
            Assert.Equal(15, NutritionCalculator.CalculateCarbs(product, 50));
        }

        [Fact]
        public void Calculate_PieceBasedProduct_UsesAverageWeight()
        {
            // Яйцо: КБЖУ на 100г, среднее яйцо = 60г
            var product = new Product
            {
                Calories = 155, Proteins = 13, Fats = 11, Carbohydrates = 1,
                IsPieceBased = true, AverageWeightGrams = 60
            };
            // 2 яйца = 2 * 60г = 120г => 155 * 120 / 100 = 186
            Assert.Equal(186, NutritionCalculator.CalculateCalories(product, 2));
            Assert.Equal(15.6, NutritionCalculator.CalculateProteins(product, 2));
        }

        [Fact]
        public void Calculate_PieceBasedProduct_NoAverageWeight_DefaultsTo100()
        {
            // Штучный продукт без указания веса — fallback = 100г
            var product = new Product
            {
                Calories = 100, Proteins = 5, Fats = 3, Carbohydrates = 15,
                IsPieceBased = true, AverageWeightGrams = null
            };
            // 1 шт = 100г => calories = 100 * 100 / 100 = 100
            Assert.Equal(100, NutritionCalculator.CalculateCalories(product, 1));
        }

        [Fact]
        public void Calculate_NullProduct_ReturnsZero()
        {
            Assert.Equal(0, NutritionCalculator.CalculateCalories(null, 100));
            Assert.Equal(0, NutritionCalculator.CalculateProteins(null, 100));
            Assert.Equal(0, NutritionCalculator.CalculateFats(null, 100));
            Assert.Equal(0, NutritionCalculator.CalculateCarbs(null, 100));
        }

        [Fact]
        public void Calculate_ZeroQuantity_ReturnsZero()
        {
            var product = new Product
            {
                Calories = 200, Proteins = 20, Fats = 10, Carbohydrates = 30,
                IsPieceBased = false
            };
            Assert.Equal(0, NutritionCalculator.CalculateCalories(product, 0));
        }

        [Fact]
        public void Calculate_ZeroNutrition_ReturnsZero()
        {
            // Продукт с КБЖУ = 0 (баг, который мы исправляем)
            var product = new Product
            {
                Calories = 0, Proteins = 0, Fats = 0, Carbohydrates = 0,
                IsPieceBased = false
            };
            Assert.Equal(0, NutritionCalculator.CalculateCalories(product, 250));
        }
    }

    // =============================================
    // Тесты NutritionService (формула Миффлина)
    // =============================================
    public class NutritionServiceTests
    {
        [Fact]
        public void CalculateTargets_Male_Medium_Maintain()
        {
            var profile = new UserProfile
            {
                Weight = 80, Height = 180, Age = 30,
                Gender = Gender.Male, ActivityLevel = ActivityLevel.Medium,
                Goal = GoalType.Maintain
            };
            var targets = NutritionService.CalculateTargets(profile);

            // BMR = 10*80 + 6.25*180 - 5*30 + 5 = 800 + 1125 - 150 + 5 = 1780
            // TDEE = 1780 * 1.55 = 2759
            Assert.Equal(2759, targets.Calories);
            Assert.True(targets.Proteins > 0);
            Assert.True(targets.Fats > 0);
            Assert.True(targets.Carbs > 0);
        }

        [Fact]
        public void CalculateTargets_Female_Low_LoseWeight()
        {
            var profile = new UserProfile
            {
                Weight = 60, Height = 165, Age = 25,
                Gender = Gender.Female, ActivityLevel = ActivityLevel.Low,
                Goal = GoalType.LoseWeight
            };
            var targets = NutritionService.CalculateTargets(profile);

            // BMR = 10*60 + 6.25*165 - 5*25 - 161 = 600 + 1031.25 - 125 - 161 = 1345.25
            // TDEE = 1345.25 * 1.2 = 1614.3
            // Deficit = 1614.3 - 300 = 1314.3 → 1314
            Assert.Equal(1314, targets.Calories);
        }

        [Fact]
        public void CalculateTargets_Male_High_GainWeight()
        {
            var profile = new UserProfile
            {
                Weight = 90, Height = 185, Age = 28,
                Gender = Gender.Male, ActivityLevel = ActivityLevel.High,
                Goal = GoalType.GainWeight
            };
            var targets = NutritionService.CalculateTargets(profile);

            // BMR = 10*90 + 6.25*185 - 5*28 + 5 = 900 + 1156.25 - 140 + 5 = 1921.25
            // TDEE = 1921.25 * 1.8 = 3458.25
            // Surplus = 3458.25 + 300 = 3758.25 → 3758
            Assert.Equal(3758, targets.Calories);
        }

        [Fact]
        public void CalculateTargets_MacrosSumApproximatelyEqualCalories()
        {
            var profile = new UserProfile
            {
                Weight = 75, Height = 175, Age = 35,
                Gender = Gender.Male, ActivityLevel = ActivityLevel.Medium,
                Goal = GoalType.Maintain
            };
            var targets = NutritionService.CalculateTargets(profile);

            // Проверяем, что Б*4 + Ж*9 + У*4 ≈ Calories (допуск ±5%)
            double macroCalories = targets.Proteins * 4 + targets.Fats * 9 + targets.Carbs * 4;
            double ratio = macroCalories / targets.Calories;
            Assert.InRange(ratio, 0.95, 1.05);
        }

        [Fact]
        public void CalculateTargets_LoseWeight_LessThanMaintain()
        {
            var profile = new UserProfile
            {
                Weight = 80, Height = 180, Age = 30,
                Gender = Gender.Male, ActivityLevel = ActivityLevel.Medium,
                Goal = GoalType.Maintain
            };
            var maintain = NutritionService.CalculateTargets(profile);

            profile.Goal = GoalType.LoseWeight;
            var lose = NutritionService.CalculateTargets(profile);

            Assert.True(lose.Calories < maintain.Calories,
                "Калории для сброса веса должны быть меньше, чем для поддержания");
            Assert.Equal(300, maintain.Calories - lose.Calories);
        }

        [Fact]
        public void CalculateTargets_GainWeight_MoreThanMaintain()
        {
            var profile = new UserProfile
            {
                Weight = 80, Height = 180, Age = 30,
                Gender = Gender.Male, ActivityLevel = ActivityLevel.Medium,
                Goal = GoalType.Maintain
            };
            var maintain = NutritionService.CalculateTargets(profile);

            profile.Goal = GoalType.GainWeight;
            var gain = NutritionService.CalculateTargets(profile);

            Assert.True(gain.Calories > maintain.Calories,
                "Калории для набора веса должны быть больше, чем для поддержания");
            Assert.Equal(300, gain.Calories - maintain.Calories);
        }
    }

    // =============================================
    // Тесты MealPlanBalancerService
    // =============================================
    public class MealPlanBalancerTests
    {
        private readonly MealPlanBalancerService _balancer = new();

        [Fact]
        public void DistributeCalories_StandardMeals_SumEqualsTarget()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "Breakfast", Title = "Завтрак" },
                new() { MealType = "Lunch", Title = "Обед" },
                new() { MealType = "Dinner", Title = "Ужин" },
                new() { MealType = "Snack", Title = "Перекус" }
            };
            int targetCalories = 2500;

            var result = _balancer.DistributeCalories(meals, targetCalories);

            Assert.Equal(4, result.Count);
            Assert.Equal(targetCalories, result.Sum(m => m.TargetCalories));
        }

        [Fact]
        public void DistributeCalories_EmptyList_ReturnsEmpty()
        {
            var result = _balancer.DistributeCalories(new List<MealStructureDto>(), 2000);
            Assert.Empty(result);
        }

        [Fact]
        public void DistributeCalories_NullList_ReturnsEmpty()
        {
            var result = _balancer.DistributeCalories(null!, 2000);
            Assert.Empty(result);
        }

        [Fact]
        public void DistributeCalories_BreakfastGets25Percent()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "Breakfast", Title = "Завтрак" },
                new() { MealType = "Lunch", Title = "Обед" },
                new() { MealType = "Dinner", Title = "Ужин" },
                new() { MealType = "Snack", Title = "Перекус" }
            };

            var result = _balancer.DistributeCalories(meals, 2000);
            var breakfast = result.First(m => m.MealType == "Breakfast");

            // ~25% = 500 ± 50 (небольшое отклонение из-за балансировки)
            Assert.InRange(breakfast.TargetCalories, 450, 550);
        }

        [Fact]
        public void DistributeCalories_LunchGetsLargestShare()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "Breakfast", Title = "Завтрак" },
                new() { MealType = "Lunch", Title = "Обед" },
                new() { MealType = "Dinner", Title = "Ужин" },
                new() { MealType = "Snack", Title = "Перекус" }
            };

            var result = _balancer.DistributeCalories(meals, 2000);
            var lunch = result.First(m => m.MealType == "Lunch");
            var maxCalories = result.Max(m => m.TargetCalories);

            Assert.Equal(maxCalories, lunch.TargetCalories);
        }

        [Fact]
        public void DistributeCalories_RussianMealTypes_Normalized()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "завтрак", Title = "Каша" },
                new() { MealType = "обед", Title = "Суп" },
                new() { MealType = "ужин", Title = "Рыба" },
                new() { MealType = "перекус", Title = "Йогурт" }
            };

            var result = _balancer.DistributeCalories(meals, 2000);

            Assert.Equal(2000, result.Sum(m => m.TargetCalories));
            Assert.Contains(result, m => m.MealType == "Breakfast");
            Assert.Contains(result, m => m.MealType == "Lunch");
        }

        [Fact]
        public void DistributeCalories_SingleMeal_GetsAllCalories()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "Lunch", Title = "Обед" }
            };

            var result = _balancer.DistributeCalories(meals, 2000);
            Assert.Single(result);
            Assert.Equal(2000, result[0].TargetCalories);
        }

        [Fact]
        public void DistributeCalories_AllMealsHavePositiveCalories()
        {
            var meals = new List<MealStructureDto>
            {
                new() { MealType = "Breakfast", Title = "Завтрак" },
                new() { MealType = "Lunch", Title = "Обед" },
                new() { MealType = "Dinner", Title = "Ужин" },
                new() { MealType = "Snack", Title = "Перекус" }
            };

            var result = _balancer.DistributeCalories(meals, 2000);
            Assert.All(result, m => Assert.True(m.TargetCalories > 0,
                $"Прием '{m.MealType}' имеет {m.TargetCalories} ккал — ожидается > 0"));
        }
    }

    // =============================================
    // Тесты ProductNameNormalizer
    // =============================================
    public class ProductNameNormalizerTests
    {
        [Theory]
        [InlineData("  Куриная грудка  ", "куриная грудка")]
        [InlineData("МОЛОКО", "молоко")]
        [InlineData("Овсяная  крупа", "овсяная крупа")]
        [InlineData("Творог нежирный", "творог нежирный")]
        public void Normalize_RemovesWhitespaceAndLowercases(string input, string expected)
        {
            Assert.Equal(expected, ProductNameNormalizer.Normalize(input));
        }

        [Theory]
        [InlineData("Творожок", "творожок")]
        [InlineData("Ёжик", "ежик")]
        public void Normalize_ReplacesYo(string input, string expected)
        {
            Assert.Equal(expected, ProductNameNormalizer.Normalize(input));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("   ", "")]
        public void Normalize_EmptyInput_ReturnsEmpty(string? input, string expected)
        {
            Assert.Equal(expected, ProductNameNormalizer.Normalize(input));
        }

        [Theory]
        [InlineData("Куриная грудка", "куриная грудка", true)]
        [InlineData("Куриная грудка", "Куриная грудка филе", true)]  // one contains the other
        [InlineData("молоко", "Молоко коровье", true)]  // one contains the other
        [InlineData("Оливковое масло", "масло оливковое", false)]  // neither contains the other fully, Levenshtein too high
        [InlineData("Рис", "Гречка", false)]
        [InlineData("Банан", "Бананы", true)]  // Levenshtein = 1
        public void AreSimilar_FuzzyMatching(string a, string b, bool expected)
        {
            Assert.Equal(expected, ProductNameNormalizer.AreSimilar(a, b));
        }

        [Fact]
        public void AreSimilar_EmptyStrings_ReturnsFalse()
        {
            Assert.False(ProductNameNormalizer.AreSimilar("", "test"));
            Assert.False(ProductNameNormalizer.AreSimilar("test", ""));
            Assert.False(ProductNameNormalizer.AreSimilar(null!, null!));
        }

        [Fact]
        public void LevenshteinDistance_SameStrings_ReturnsZero()
        {
            Assert.Equal(0, ProductNameNormalizer.LevenshteinDistance("test", "test"));
        }

        [Fact]
        public void LevenshteinDistance_OneCharDifference_ReturnsOne()
        {
            Assert.Equal(1, ProductNameNormalizer.LevenshteinDistance("банан", "бананы"));
        }

        [Fact]
        public void LevenshteinDistance_EmptyString_ReturnsLength()
        {
            Assert.Equal(5, ProductNameNormalizer.LevenshteinDistance("", "hello"));
            Assert.Equal(3, ProductNameNormalizer.LevenshteinDistance("abc", ""));
        }
    }

    // =============================================
    // Тесты RecipeNutritionAdjuster
    // =============================================
    public class RecipeNutritionAdjusterTests
    {
        private static Recipe CreateTestRecipe(
            double calories, double proteins, double fats, double carbs,
            double quantity)
        {
            var product = new Product
            {
                Calories = calories, Proteins = proteins,
                Fats = fats, Carbohydrates = carbs,
                IsPieceBased = false
            };
            return new Recipe
            {
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new RecipeIngredient { Product = product, Quantity = quantity }
                }
            };
        }

        [Fact]
        public void CalculateNutrition_SimpleRecipe_CorrectValues()
        {
            // 200г куриной грудки (180 ккал на 100г)
            var recipe = CreateTestRecipe(180, 23, 8, 0, 200);
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            Assert.Equal(360, nutrition.Calories);
            Assert.Equal(46, nutrition.Proteins);
            Assert.Equal(16, nutrition.Fats);
            Assert.Equal(0, nutrition.Carbs);
        }

        [Fact]
        public void CalculateNutrition_EmptyRecipe_ReturnsZero()
        {
            var recipe = new Recipe { RecipeIngredients = new List<RecipeIngredient>() };
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            Assert.Equal(0, nutrition.Calories);
        }

        [Fact]
        public void CalculateNutrition_MultipleIngredients_SumsCorrectly()
        {
            var chicken = new Product
            {
                Calories = 180, Proteins = 23, Fats = 8, Carbohydrates = 0,
                IsPieceBased = false
            };
            var rice = new Product
            {
                Calories = 330, Proteins = 7, Fats = 1, Carbohydrates = 74,
                IsPieceBased = false
            };
            var recipe = new Recipe
            {
                RecipeIngredients = new List<RecipeIngredient>
                {
                    new() { Product = chicken, Quantity = 200 },   // 360 ккал
                    new() { Product = rice, Quantity = 100 }       // 330 ккал
                }
            };

            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            Assert.Equal(690, nutrition.Calories);
            Assert.Equal(53, nutrition.Proteins);    // 46 + 7
        }

        [Fact]
        public void CalculateNutrition_ZeroKBJUProduct_ReturnsZero()
        {
            // Продукт с КБЖУ = 0 (проблема, которую мы исправляем)
            var recipe = CreateTestRecipe(0, 0, 0, 0, 200);
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            Assert.Equal(0, nutrition.Calories);
        }

        [Fact]
        public void IsWithinTolerance_ExactMatch_ReturnsTrue()
        {
            var target = new MealNutrition(500, 30, 15, 60);
            var actual = new MealNutrition(500, 30, 15, 60);
            Assert.True(RecipeNutritionAdjuster.IsWithinTolerance(target, actual));
        }

        [Fact]
        public void IsWithinTolerance_Within3Percent_ReturnsTrue()
        {
            var target = new MealNutrition(500, 30, 15, 60);
            var actual = new MealNutrition(510, 31, 14, 62);
            // 510/500 = 2% deviation — within 3% tolerance
            Assert.True(RecipeNutritionAdjuster.IsWithinTolerance(target, actual));
        }

        [Fact]
        public void IsWithinTolerance_Beyond3Percent_ReturnsFalse()
        {
            var target = new MealNutrition(500, 30, 15, 60);
            var actual = new MealNutrition(550, 35, 18, 70);
            // 550/500 = 10% deviation — beyond 3%
            Assert.False(RecipeNutritionAdjuster.IsWithinTolerance(target, actual));
        }

        [Fact]
        public void IsWithinTolerance_ZeroTarget_ReturnsTrue()
        {
            var target = new MealNutrition(0, 0, 0, 0);
            var actual = new MealNutrition(500, 30, 15, 60);
            Assert.True(RecipeNutritionAdjuster.IsWithinTolerance(target, actual));
        }

        [Fact]
        public void BuildTargetNutrition_ReturnsCorrectMacros()
        {
            var nutrition = RecipeNutritionAdjuster.BuildTargetNutrition(2000);

            Assert.Equal(2000, nutrition.Calories);
            // P = 2000 * 0.30 / 4 = 150
            Assert.Equal(150, nutrition.Proteins);
            // F = 2000 * 0.25 / 9 ≈ 55.56
            Assert.InRange(nutrition.Fats, 55, 56);
            // C = 2000 * 0.45 / 4 = 225
            Assert.Equal(225, nutrition.Carbs);
        }
    }
}

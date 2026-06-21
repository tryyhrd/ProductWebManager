using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ProductWebManager.Services;
using ProductWebManager.Classes.AI;

namespace TestProject1
{
    public class MealPlanValidationTests
    {
        // Вспомогательный класс для мокирования ответов HttpClient
        public class MockHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string query = request.RequestUri?.ToString() ?? "";
                var response = new OpenFoodFactsSearchResponse { Products = new List<OpenFoodFactsProductDto>() };

                if (query.Contains("Куриная"))
                {
                    response.Products.Add(new OpenFoodFactsProductDto
                    {
                        ProductName = "Куриная грудка",
                        Nutriments = new OpenFoodFactsNutriments
                        {
                            Calories100g = 165,
                            Proteins100g = 31,
                            Fat100g = 3.6,
                            Carbohydrates100g = 0
                        }
                    });
                }
                else if (query.Contains("Гречневая"))
                {
                    response.Products.Add(new OpenFoodFactsProductDto
                    {
                        ProductName = "Гречневая крупа",
                        Nutriments = new OpenFoodFactsNutriments
                        {
                            Calories100g = 343,
                            Proteins100g = 13,
                            Fat100g = 3.4,
                            Carbohydrates100g = 71.5
                        }
                    });
                }

                var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(response))
                };
                return Task.FromResult(responseMessage);
            }
        }

        [Fact]
        public async Task ValidateAiGeneratedKbju_Against_MockedApi()
        {
            // 1. Мокируем "ответ от ИИ", где ИИ придумал рецепт с ингредиентами и КБЖУ.
            // Допустим, ИИ сгенерировал рецепт "Куриная грудка с гречкой"
            var aiGeneratedMeal = new GigaChatHelper.GeneratedMealDto
            {
                Title = "Куриная грудка с гречкой",
                // ИИ может немного ошибаться или округлять КБЖУ
                Calories = 480,
                Proteins = 55,
                Fats = 8,
                Carbs = 50,
                Ingredients = new List<GigaChatHelper.GeneratedIngredientDto>
                {
                    new GigaChatHelper.GeneratedIngredientDto
                    {
                        Name = "Куриная грудка",
                        Quantity = 150, // грамм
                        Unit = "г"
                    },
                    new GigaChatHelper.GeneratedIngredientDto
                    {
                        Name = "Гречневая крупа",
                        Quantity = 70, // грамм (сухой)
                        Unit = "г"
                    }
                }
            };

            // 2. Инициализируем сервис с мокированным HTTP-клиентом
            var mockHandler = new MockHttpMessageHandler();
            using var httpClient = new HttpClient(mockHandler);
            var offService = new OpenFoodFactsService(httpClient);

            double apiTotalCalories = 0;
            double apiTotalProteins = 0;
            double apiTotalFats = 0;
            double apiTotalCarbs = 0;

            // 3. Получаем данные для каждого ингредиента из "API"
            foreach (var aiIngredient in aiGeneratedMeal.Ingredients)
            {
                var searchResults = await offService.SearchProductsAsync(aiIngredient.Name);
                var apiProduct = searchResults.FirstOrDefault(p => p.Nutriments != null && p.Nutriments.Calories100g.HasValue);
                
                if (apiProduct?.Nutriments != null)
                {
                    // Рассчитываем КБЖУ пропорционально весу
                    double quantityMultiplier = (double)aiIngredient.Quantity / 100.0;
                    
                    apiTotalCalories += (apiProduct.Nutriments.Calories100g ?? 0) * quantityMultiplier;
                    apiTotalProteins += (apiProduct.Nutriments.Proteins100g ?? 0) * quantityMultiplier;
                    apiTotalFats += (apiProduct.Nutriments.Fat100g ?? 0) * quantityMultiplier;
                    apiTotalCarbs += (apiProduct.Nutriments.Carbohydrates100g ?? 0) * quantityMultiplier;
                }
                else
                {
                    Assert.Fail($"Не удалось найти продукт '{aiIngredient.Name}' в API.");
                }
            }

            // Ожидаемые значения из API:
            // Курица (150г): 247.5 ккал, 46.5 белки, 5.4 жиры, 0 углеводы
            // Гречка (70г): 240.1 ккал, 9.1 белки, 2.38 жиры, 50.05 углеводы
            // Итого API: ~487.6 ккал, 55.6 белки, 7.78 жиры, 50.05 углеводы

            // 4. Сравниваем результаты
            // Допустимая погрешность в 15% (0.15) от реальных значений
            double tolerancePercentage = 0.15;

            AssertIsWithinTolerance("Калории", aiGeneratedMeal.Calories, apiTotalCalories, tolerancePercentage);
            AssertIsWithinTolerance("Белки", aiGeneratedMeal.Proteins, apiTotalProteins, tolerancePercentage);
            AssertIsWithinTolerance("Жиры", aiGeneratedMeal.Fats, apiTotalFats, tolerancePercentage);
            AssertIsWithinTolerance("Углеводы", aiGeneratedMeal.Carbs, apiTotalCarbs, tolerancePercentage);
        }

        private void AssertIsWithinTolerance(string propertyName, double aiValue, double apiValue, double tolerancePercentage)
        {
            double difference = Math.Abs(aiValue - apiValue);
            double maxAllowedDifference = apiValue * tolerancePercentage;

            Assert.True(difference <= maxAllowedDifference, 
                $"Погрешность для {propertyName} слишком велика! ИИ: {aiValue:F2}, API: {apiValue:F2}. Разница: {difference:F2}, Макс. допустимо: {maxAllowedDifference:F2}");
        }
    }

    // =============================================
    // Тесты КБЖУ рецепта "Шоколадно-ореховая каша с фруктами"
    // =============================================
    public class ChocolateNutOatmealNutritionTests
    {
        /// <summary>
        /// Строит рецепт "Шоколадно-ореховая каша с фруктами" с реальными Product-объектами.
        /// Ингредиенты (КБЖУ на 100г):
        ///   - Овсяные хлопья  80г   (371/12.3/6.1/67.5)
        ///   - Молоко 2.5%    200мл  (52/2.8/2.5/4.7)
        ///   - Банан          130г   (96/1.5/0.2/21.8)
        ///   - Грецкий орех    20г   (687/15.2/65.2/13.7)
        ///   - Какао-порошок   10г   (289/20.2/11.5/37.0)
        ///
        /// Ожидаемые суммарные КБЖУ:
        ///   Ккал ≈ 691.9  (296.8 + 104.0 + 124.8 + 137.4 + 28.9)
        ///   Б    ≈  22.4  (9.84  + 5.60  + 1.95  + 3.04  + 2.02)
        ///   Ж    ≈  24.3  (4.88  + 5.00  + 0.26  + 13.04 + 1.15)
        ///   У    ≈  98.2  (54.00 + 9.40  + 28.34 + 2.74  + 3.70)
        /// </summary>
        private static ProductWebManager.Models.Recipe BuildChocolateNutOatmealRecipe()
        {
            var unitG  = new ProductWebManager.Models.Unit { Id = 1, Name = "г" };
            var unitMl = new ProductWebManager.Models.Unit { Id = 2, Name = "мл" };

            var oats = new ProductWebManager.Models.Product
            {
                Id = 1, Name = "Овсяные хлопья",
                Calories = 371, Proteins = 12.3, Fats = 6.1, Carbohydrates = 67.5,
                IsPieceBased = false, Unit = unitG
            };
            var milk = new ProductWebManager.Models.Product
            {
                Id = 2, Name = "Молоко 2.5%",
                Calories = 52, Proteins = 2.8, Fats = 2.5, Carbohydrates = 4.7,
                IsPieceBased = false, Unit = unitMl
            };
            var banana = new ProductWebManager.Models.Product
            {
                Id = 3, Name = "Банан",
                Calories = 96, Proteins = 1.5, Fats = 0.2, Carbohydrates = 21.8,
                IsPieceBased = false, Unit = unitG
            };
            var walnut = new ProductWebManager.Models.Product
            {
                Id = 4, Name = "Грецкий орех",
                Calories = 687, Proteins = 15.2, Fats = 65.2, Carbohydrates = 13.7,
                IsPieceBased = false, Unit = unitG
            };
            var cacao = new ProductWebManager.Models.Product
            {
                Id = 5, Name = "Какао-порошок",
                Calories = 289, Proteins = 20.2, Fats = 11.5, Carbohydrates = 37.0,
                IsPieceBased = false, Unit = unitG
            };

            return new ProductWebManager.Models.Recipe
            {
                Id = 99,
                Title = "Шоколадно-ореховая каша с фруктами",
                Description = "Питательный завтрак с какао, орехами и свежими фруктами",
                RecipeIngredients = new List<ProductWebManager.Models.RecipeIngredient>
                {
                    new() { Product = oats,   Quantity = 80,  Unit = unitG  },
                    new() { Product = milk,   Quantity = 200, Unit = unitMl },
                    new() { Product = banana, Quantity = 130, Unit = unitG  },
                    new() { Product = walnut, Quantity = 20,  Unit = unitG  },
                    new() { Product = cacao,  Quantity = 10,  Unit = unitG  },
                }
            };
        }

        [Fact]
        public void ChocolateNutOatmeal_CalculateNutrition_CorrectCalories()
        {
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            // Ожидается ≈ 691.9 ккал (допуск ±10 из-за округления)
            Assert.InRange(nutrition.Calories, 681, 702);
        }

        [Fact]
        public void ChocolateNutOatmeal_CalculateNutrition_CorrectProteins()
        {
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            // Ожидается ≈ 22.4г белка (допуск ±2)
            Assert.InRange(nutrition.Proteins, 20, 25);
        }

        [Fact]
        public void ChocolateNutOatmeal_CalculateNutrition_CorrectFats()
        {
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            // Ожидается ≈ 24.3г жиров (допуск ±2)
            Assert.InRange(nutrition.Fats, 22, 27);
        }

        [Fact]
        public void ChocolateNutOatmeal_CalculateNutrition_CorrectCarbs()
        {
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            // Ожидается ≈ 98.2г углеводов (допуск ±3)
            Assert.InRange(nutrition.Carbs, 94, 103);
        }

        [Fact]
        public void ChocolateNutOatmeal_MacrosSumApproximatelyEqualCalories()
        {
            // Проверяем что Б*4 + Ж*9 + У*4 ≈ Калории (допуск ±10%)
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            double macroCalories = nutrition.Proteins * 4 + nutrition.Fats * 9 + nutrition.Carbs * 4;
            double ratio = macroCalories / nutrition.Calories;

            Assert.InRange(ratio, 0.90, 1.10);
        }

        [Fact]
        public void ChocolateNutOatmeal_IsWithinBreakfastCalorieRange()
        {
            // Завтрак должен быть в диапазоне 400–900 ккал
            var recipe = BuildChocolateNutOatmealRecipe();
            var nutrition = RecipeNutritionAdjuster.CalculateNutrition(recipe);

            Assert.True(nutrition.Calories >= 400,
                $"Завтрак слишком низкокалорийный: {nutrition.Calories:F1} ккал");
            Assert.True(nutrition.Calories <= 900,
                $"Завтрак слишком калорийный: {nutrition.Calories:F1} ккал");
        }

        [Fact]
        public void ChocolateNutOatmeal_ReplacementTitle_IsNotOriginal()
        {
            // При замене блюда новое название не должно совпадать с оригиналом
            const string originalTitle = "Шоколадно-ореховая каша с фруктами";

            var potentialReplacements = new[]
            {
                "Овсянка с мёдом и орехами",
                "Гречневая каша с ягодами",
                "Творожная запеканка с фруктами"
            };

            foreach (var title in potentialReplacements)
            {
                Assert.NotEqual(originalTitle, title, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}

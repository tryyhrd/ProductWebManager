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
}

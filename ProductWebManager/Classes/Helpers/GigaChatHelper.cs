using Newtonsoft.Json;
using System.Text;
using System.Net.Http.Headers;
using ProductWebManager.Models;

namespace ProductWebManager.Classes.Helpers
{
    public static class GigaChatHelper
    {
        private const string ClientId = "019bca1f-0f50-72b2-b33b-7fb5c3b89be6";
        private const string AuthorizationKey = "MDE5YmNhMWYtMGY1MC03MmIyLWIzM2ItN2ZiNWMzYjg5YmU2OjE2NjQxYWQ0LWVhMjctNDYzYi1hYjRmLTRjZTI4ZDU1NTVkOA==";

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
        })
        { Timeout = TimeSpan.FromMinutes(5) };

        private static string _cachedToken = "";
        private static DateTime _tokenExpiry = DateTime.MinValue;

        public static async Task<GeneratedMenuDto?> GenerateAndParseMenuAsync(List<FridgeItem> fridgeItems, int daysCount)
        {
            var finalMenu = new GeneratedMenuDto
            {
                MenuName = "Сгенерированное меню",
                Items = new List<GeneratedMenuItemDto>()
            };

            for (int i = 1; i <= daysCount; i++)
            {
                string systemPrompt = CreateSingleDayPrompt(fridgeItems, i);

                var messages = new List<Message>
                {
                    new Message { role = "user", content = systemPrompt }
                };

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        string token = await GetToken();
                        var response = await GetAnswer(token, messages);

                        if (response?.choices?.Count > 0)
                        {
                            string rawContent = response.choices[0].message.content;
                            string json = CleanJson(rawContent);

                            var dayMenu = JsonConvert.DeserializeObject<GeneratedMenuDto>(json);

                            if (dayMenu?.Items != null && dayMenu.Items.Any())
                            {
                                foreach (var item in dayMenu.Items) item.DayNumber = i;
                                finalMenu.Items.AddRange(dayMenu.Items);
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка генерации дня {i} (попытка {attempt}): {ex.Message}");
                        await Task.Delay(1500);
                    }
                }
            }

            return finalMenu.Items.Count > 0 ? finalMenu : null;
        }

        private static async Task<ResponseMessage?> GetAnswer(string token, List<Message> messages)
        {
            string url = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";

            var requestData = new
            {
                model = "GigaChat",
                stream = false,
                repetition_penalty = 1,
                messages = messages,
                temperature = 0.25
            };

            var jsonContent = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-Client-ID", ClientId);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string errorBody = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API Error ({response.StatusCode}): {errorBody}");
                return null;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<ResponseMessage>(responseContent);
        }

        private static async Task<string> GetToken()
        {
            if (!string.IsNullOrEmpty(_cachedToken) && _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
                return _cachedToken;

            string url = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
            string rqUid = Guid.NewGuid().ToString();

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", AuthorizationKey);
            request.Headers.Add("RqUID", rqUid);

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            };
            request.Content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Не удалось получить токен: {response.StatusCode}. {error}");
            }

            string responseString = await response.Content.ReadAsStringAsync();
            var tokenData = JsonConvert.DeserializeObject<dynamic>(responseString);

            _cachedToken = tokenData.access_token;
            long expiresAt = tokenData.expires_at;
            _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt).UtcDateTime;

            return _cachedToken;
        }

        private static string CleanJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            json = json.Replace("```json", "").Replace("```", "").Trim();

            int firstBrace = json.IndexOf('{');
            int lastBrace = json.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
                json = json.Substring(firstBrace, lastBrace - firstBrace + 1);

            return json;
        }

        private static string CreateSingleDayPrompt(List<FridgeItem> items, int dayNumber)
        {
            var productNames = items
                .Where(i => i.Product != null)
                .Select(i => $"\"{i.Product!.Name}\"");
            string ingredientsString = string.Join(", ", productNames);

            return $@"
Ты — профессиональный шеф-повар и нутрициолог. Составь меню на ДЕНЬ №{dayNumber}, используя предоставленные продукты.

ПРОДУКТЫ В НАЛИЧИИ: {ingredientsString}

ПРАВИЛА:
1. Только валидный JSON, без Markdown и лишнего текста.
2. 3 приёма пищи: Завтрак, Обед, Ужин.
3. Используй продукты из списка выше.
4. Каждый ингредиент должен иметь поле category.

ФОРМАТ ОТВЕТА:
{{
  ""items"": [
    {{
      ""dayNumber"": {dayNumber},
      ""mealType"": ""Завтрак"",
      ""recipe"": {{
        ""title"": ""Название блюда"",
        ""description"": ""Краткое описание"",
        ""calories"": 350,
        ""prepTime"": 10,
        ""cookTime"": 15,
        ""instructions"": [""Шаг 1"", ""Шаг 2""],
        ""ingredients"": [
          {{
            ""name"": ""Ингредиент"",
            ""quantity"": 100,
            ""unit"": ""г"",
            ""category"": ""Категория""
          }}
        ]
      }}
    }}
  ]
}}";
        }

        public class GeneratedMenuDto
        {
            public string MenuName { get; set; } = "";
            public List<GeneratedMenuItemDto> Items { get; set; } = new();
        }

        public class GeneratedMenuItemDto
        {
            public int DayNumber { get; set; }
            public string MealType { get; set; } = "";
            public GeneratedRecipeDto Recipe { get; set; } = new();
        }

        public class GeneratedRecipeDto
        {
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public List<string> Instructions { get; set; } = new();
            public int Calories { get; set; }
            public int PrepTime { get; set; }
            public int CookTime { get; set; }
            public List<GeneratedIngredientDto> Ingredients { get; set; } = new();
        }

        public class GeneratedIngredientDto
        {
            public string Name { get; set; } = "";
            public decimal Quantity { get; set; }
            public string Unit { get; set; } = "";
            public string Category { get; set; } = "";
        }

        public class ResponseMessage
        {
            public List<Choice> choices { get; set; } = new();
            public class Choice
            {
                public string finish_reason { get; set; } = "";
                public int index { get; set; }
                public Message message { get; set; } = new();
            }
        }

        public class Message
        {
            public string role { get; set; } = "";
            public string content { get; set; } = "";
        }
    }
}
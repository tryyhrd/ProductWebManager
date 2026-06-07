using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ProductWebManager.Models;
using ProductWebManager.Services;

namespace ProductWebManager.Classes.AI;

public sealed class GigaChatHelper
{
    private readonly HttpClient _httpClient;
    private readonly GigaChatOptions _options;
    private readonly ILogger<GigaChatHelper> _logger;

    private static readonly SemaphoreSlim _tokenLock = new(1, 1);
    private static string _cachedToken = string.Empty;
    private static DateTime _tokenExpiry = DateTime.MinValue;

    public GigaChatHelper(
        HttpClient httpClient,
        IOptions<GigaChatOptions> options,
        ILogger<GigaChatHelper> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<MealPlanStructureDto?> GenerateMealPlanStructureAsync(
        UserProfile profile,
        int days,
        GoalType goal)
    {
        var calories = CalculateTargetCalories(profile, goal);
        var proteins = (int)Math.Round(calories * 0.30 / 4.0);
        var fats = (int)Math.Round(calories * 0.25 / 9.0);
        var carbs = (int)Math.Round(calories * 0.45 / 4.0);

        return await GenerateMealPlanStructureAsync(
            profile, days, goal, calories, proteins, fats, carbs,
            useFridgeProducts: false, generationMode: "balanced",
            existingRecipeTitles: null, fridgeProductNames: null);
    }

    public async Task<MealPlanStructureDto?> GenerateMealPlanStructureAsync(
        UserProfile profile,
        int days,
        GoalType goal,
        int targetCalories,
        int targetProteins,
        int targetFats,
        int targetCarbs,
        bool useFridgeProducts,
        string generationMode,
        IEnumerable<string>? existingRecipeTitles,
        IEnumerable<string>? fridgeProductNames)
    {
        var recipeTitles = LimitAndNormalize(existingRecipeTitles, 40);
        var fridgeProducts = LimitAndNormalize(fridgeProductNames, 30);

        var systemPrompt =
            "Ты профессиональный нутрициолог и шеф-повар. " +
            "Ты создаёшь реалистичную структуру плана питания. " +
            "Верни ТОЛЬКО валидный JSON без пояснений.";

        var targets = NutritionService.CalculateTargets(profile);

        var userPrompt = $@"
Составь план питания на {days} дней.
ЦЕЛЕВЫЕ ПОКАЗАТЕЛИ В СУТКИ: {targets.Calories} ккал (Б:{targets.Proteins}г, Ж:{targets.Fats}г, У:{targets.Carbs}г).

ПРАВИЛА БАЛАНСИРОВКИ:
1. Сумма калорий 4 приемов (Breakfast, Lunch, Dinner, Snack) должна строго равняться {targets.Calories}.
2. РАСПРЕДЕЛЕНИЕ: Завтрак ~25%, Обед ~35%, Ужин ~25%, Перекус ~15%.
3. ХОЛОДИЛЬНИК: Если переданы продукты ({string.Join(", ", fridgeProducts)}), они ОБЯЗАТЕЛЬНО должны быть основными ингредиентами минимум в 2 приемах пищи в день.
4. КУЛИНАРНАЯ ЛОГИКА: Не предлагай '120г яблока', если можно предложить '1 среднее яблоко (150г)'. Подгоняй КБЖУ за счет круп и масел.

ОБЯЗАТЕЛЬНО: Для каждого ингредиента укажи КБЖУ НА 100 ГРАММ (или на 1 штуку для штучных продуктов).

ВЕРНИ JSON:
{{
  ""days"": [
    {{
      ""day"": 1,
      ""meals"": [
        {{
          ""type"": ""Lunch"",
          ""title"": ""Название"",
          ""calories"": 500,
          ""proteins"": 30,
          ""fats"": 15,
          ""carbs"": 60,
          ""description"": ""Описание блюда"",
          ""instructions"": [""Шаг 1"", ""Шаг 2""],
          ""ingredients"": [{{ ""name"": ""Продукт"", ""quantity"": 150, ""unit"": ""г"", ""category"": ""Категория"", ""calories"": 130, ""proteins"": 11, ""fats"": 2, ""carbs"": 20 }}]
        }}
      ]
    }}
  ]
}}";

        try
        {
            var raw = await SendPromptAsync(systemPrompt, userPrompt);
            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("ИИ вернул пустой ответ для структуры плана");
                return null;
            }

            var json = ExtractJson(raw);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Не удалось извлечь JSON из ответа ИИ");
                return null;
            }

            var dto = JsonConvert.DeserializeObject<MealPlanStructureDto>(
                json,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

            if (dto == null || dto.Days == null || dto.Days.Count == 0)
            {
                _logger.LogWarning("Десериализованная структура плана пуста или невалидна");
                return null;
            }

            dto.Name = string.IsNullOrWhiteSpace(dto.Name)
                ? $"План питания {DateTime.Now:dd.MM.yyyy}"
                : dto.Name.Trim();

            foreach (var day in dto.Days)
            {
                day.Meals ??= new List<MealStructureDto>();
                foreach (var meal in day.Meals)
                {
                    meal.Title = string.IsNullOrWhiteSpace(meal.Title) ? "Блюдо" : meal.Title.Trim();
                    meal.MealType = NormalizeMealType(meal.MealType);

                    if (meal.TargetCalories <= 0)
                        meal.TargetCalories = targetCalories / 4;

                    // Фильтрация невалидных ингредиентов
                    meal.Ingredients = (meal.Ingredients ?? new List<GeneratedIngredientDto>())
                        .Where(i => !string.IsNullOrWhiteSpace(i.Name) && i.Quantity > 0)
                        .Select(i =>
                        {
                            i.Name = i.Name.Trim();
                            // Гарантируем неотрицательные значения КБЖУ
                            i.Calories = Math.Max(0, i.Calories);
                            i.Proteins = Math.Max(0, i.Proteins);
                            i.Fats = Math.Max(0, i.Fats);
                            i.Carbs = Math.Max(0, i.Carbs);
                            return i;
                        })
                        .ToList();
                }
            }

            _logger.LogInformation("Успешно получена структура плана: {Days} дней", dto.Days.Count);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации структуры плана");
            return null;
        }
    }

    public async Task<GeneratedMealDto?> GenerateRecipeByTitleAsync(
        string title,
        int targetCalories,
        bool isSnack,
        string? mealType,
        UserProfile? profile,
        GoalType? goal)
    {
        var systemPrompt =
            "Ты профессиональный шеф-повар и нутрициолог. " +
            "Создаёшь один реалистичный рецепт с точными ингредиентами. " +
            "Верни ТОЛЬКО валидный JSON без markdown и пояснений.";

        var goalText = goal?.ToString() ?? "не задана";
        var mealTypeText = string.IsNullOrWhiteSpace(mealType)
            ? (isSnack ? "Snack" : "Main")
            : mealType;

        var userPrompt = $$"""
Создай рецепт: "{{title}}".

ТРЕБОВАНИЯ:
1. ТОЛЬКО валидный JSON, без markdown.
2. Ингредиенты реалистичные, с точными количествами.
3. Общая калорийность ≈ {{targetCalories}} ккал (допуск ±3%).
4. {{(isSnack ? "Перекус: простой, быстрый, до 200 ккал." : "Основной приём: сбалансированный, сытный.")}}
5. Укажи КБЖУ на ВЕСЬ рецепт.
6. Для каждого ингредиента: name, quantity, unit, category, calories/proteins/fats/carbs на 100г/1шт.
7. Название оставь близким к "{{title}}", если логично.

ДОПОЛНИТЕЛЬНО:
- Тип: {{mealTypeText}}, Перекус: {{(isSnack ? "да" : "нет")}}, Цель: {{goalText}}

ФОРМАТ JSON:
{
  "mealType": "Breakfast",
  "title": "Овсянка с бананом",
  "description": "Полезный завтрак",
  "isSnack": false,
  "calories": 420,
  "proteins": 18,
  "fats": 10,
  "carbs": 58,
  "ingredients": [
    {
      "name": "Овсянка",
      "quantity": 80,
      "unit": "г",
      "category": "Бакалея",
      "calories": 352,
      "proteins": 11.9,
      "fats": 5.8,
      "carbs": 65.4
    }
  ],
  "instructions": ["Шаг 1", "Шаг 2"]
}
""";

        try
        {
            var raw = await SendPromptAsync(systemPrompt, userPrompt);
            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("ИИ вернул пустой ответ для рецепта \"{Title}\"", title);
                return null;
            }

            var json = ExtractJson(raw);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Не удалось извлечь JSON для рецепта \"{Title}\"", title);
                return null;
            }

            var dto = JsonConvert.DeserializeObject<GeneratedMealDto>(
                json,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

            if (dto == null)
            {
                _logger.LogWarning("Десериализация рецепта \"{Title}\" вернула null", title);
                return null;
            }

            // Пост-обработка
            dto.Title = string.IsNullOrWhiteSpace(dto.Title) ? title.Trim() : dto.Title.Trim();
            dto.Description ??= string.Empty;
            dto.Instructions ??= new List<string>();
            dto.Ingredients ??= new List<GeneratedIngredientDto>();
            dto.MealType = NormalizeMealType(dto.MealType);

            if (dto.Calories <= 0)
                dto.Calories = targetCalories;

            _logger.LogDebug("Сгенерирован рецепт: {Title} ({Calories} ккал)", dto.Title, dto.Calories);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при генерации рецепта \"{Title}\"", title);
            return null;
        }
    }

    public async Task<string?> SendPromptAsync(string systemPrompt, string userPrompt)
    {
        try
        {
            var token = await GetTokenAsync();

            var request = new
            {
                model = "GigaChat",
                stream = false,
                repetition_penalty = 1.1,
                temperature = 0.35,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                }
            };

            var json = JsonConvert.SerializeObject(request);

            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://gigachat.devices.sberbank.ru/api/v1/chat/completions");

            httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            httpRequest.Headers.Add("X-Client-ID", _options.ClientId);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(httpRequest);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("GigaChat API error {StatusCode}: {Content}",
                    response.StatusCode, content);
                return null;
            }

            var parsed = JsonConvert.DeserializeObject<ResponseMessage>(content);
            return parsed?.choices?.FirstOrDefault()?.message?.content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при отправке запроса к GigaChat");
            return null;
        }
    }

    private async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) &&
            _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync();
        try
        {
            if (!string.IsNullOrWhiteSpace(_cachedToken) &&
                _tokenExpiry > DateTime.UtcNow.AddMinutes(1))
            {
                return _cachedToken;
            }

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.AuthKey);
            request.Headers.Add("RqUID", Guid.NewGuid().ToString());
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("scope", "GIGACHAT_API_PERS")
            });

            var response = await _httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Не удалось получить токен: {content}");

            var tokenData = JsonConvert.DeserializeObject<TokenResponse>(content);
            if (tokenData == null || string.IsNullOrWhiteSpace(tokenData.access_token))
                throw new InvalidOperationException("Пустой access_token");

            _cachedToken = tokenData.access_token;
            _tokenExpiry = DateTimeOffset.FromUnixTimeMilliseconds(tokenData.expires_at).UtcDateTime;

            _logger.LogDebug("Получен новый токен GigaChat, истекает: {Expiry}", _tokenExpiry);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static int CalculateTargetCalories(UserProfile profile, GoalType goal)
    {
        var bmr = profile.Gender == Gender.Male
            ? 10 * profile.Weight + 6.25 * profile.Height - 5 * profile.Age + 5
            : 10 * profile.Weight + 6.25 * profile.Height - 5 * profile.Age - 161;

        var activityMultiplier = profile.ActivityLevel switch
        {
            ActivityLevel.Low => 1.2,
            ActivityLevel.Medium => 1.55,
            ActivityLevel.High => 1.8,
            _ => 1.2
        };

        var calories = bmr * activityMultiplier;

        calories = goal switch
        {
            GoalType.LoseWeight => calories - 300,
            GoalType.GainWeight => calories + 300,
            _ => calories
        };

        return (int)Math.Round(calories);
    }

    private static string ExtractJson(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        text = text.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                   .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                   .Trim();

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');

        return (start >= 0 && end > start) ? text[start..(end + 1)] : text;
    }

    private static string NormalizeMealType(string? value)
    {
        var v = NormalizeText(value);

        return v switch
        {
            var s when s.Contains("завтрак") || s.Contains("breakfast") => "Breakfast",
            var s when s.Contains("обед") || s.Contains("lunch") => "Lunch",
            var s when s.Contains("ужин") || s.Contains("dinner") => "Dinner",
            var s when s.Contains("перекус") || s.Contains("snack") => "Snack",
            _ => string.IsNullOrWhiteSpace(value) ? "Snack" : value.Trim()
        };
    }

    private static List<string> LimitAndNormalize(IEnumerable<string>? items, int limit)
    {
        if (items == null)
            return new List<string>();

        return items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeText)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(limit)
            .ToList();
    }

    private static string NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Trim().ToLowerInvariant().Replace("ё", "е");
    }

    // В класс GigaChatHelper добавьте новый метод:

    public async Task<GeneratedMealDto?> FindReplacementRecipeAsync(
        string originalTitle,
        MealType mealType,
        int targetCalories,
        bool isSnack,
        UserProfile? profile,
        GoalType? goal,
        IEnumerable<string>? existingRecipeTitles = null,
        IEnumerable<string>? fridgeProductNames = null)
    {
        var existingTitles = LimitAndNormalize(existingRecipeTitles, 30);
        var fridgeProducts = LimitAndNormalize(fridgeProductNames, 20);

        var systemPrompt =
            "Ты профессиональный шеф-повар и нутрициолог. " +
            "Находишь альтернативную замену блюду. " +
            "Верни ТОЛЬКО валидный JSON без markdown и пояснений.";

        var userPrompt = $$"""
Найди ЗАМЕНУ для блюда "{{originalTitle}}".

ТРЕБОВАНИЯ:
1. ТОЛЬКО валидный JSON, без markdown.
2. НЕ повторяй оригинальное блюдо.
3. Другое название, другие основные ингредиенты.
4. Общая калорийность ≈ {{targetCalories}} ккал (допуск ±3%).
5. Тип приёма: {{mealType}}, Перекус: {{(isSnack ? "да" : "нет")}}.
6. Укажи КБЖУ на ВЕСЬ рецепт.
7. Для каждого ингредиента: name, quantity, unit, category, calories/proteins/fats/carbs на 100г/1шт.

{{(existingTitles.Count > 0 ? "НЕ ИСПОЛЬЗУЙ эти названия: " + string.Join(", ", existingTitles.Take(20)) : "")}}
{{(fridgeProducts.Count > 0 ? "Используй эти продукты: " + string.Join(", ", fridgeProducts) : "")}}
{{(goal.HasValue ? "Цель: " + goal.Value.ToString() : "")}}

ФОРМАТ JSON:
{
  "mealType": "{{mealType}}",
  "title": "Название замены",
  "description": "Краткое описание",
  "isSnack": {{(isSnack ? "true" : "false")}},
  "calories": {{targetCalories}},
  "proteins": 20,
  "fats": 15,
  "carbs": 50,
  "ingredients": [
    {
      "name": "Ингредиент",
      "quantity": 100,
      "unit": "г",
      "category": "Категория",
      "calories": 100,
      "proteins": 10,
      "fats": 5,
      "carbs": 20
    }
  ],
  "instructions": ["Шаг 1", "Шаг 2"]
}
""";

        try
        {
            var raw = await SendPromptAsync(systemPrompt, userPrompt);
            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning("ИИ вернул пустой ответ для замены \"{Title}\"", originalTitle);
                return null;
            }

            var json = ExtractJson(raw);
            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("Не удалось извлечь JSON для замены \"{Title}\"", originalTitle);
                return null;
            }

            var dto = JsonConvert.DeserializeObject<GeneratedMealDto>(
                json,
                new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

            if (dto == null)
            {
                _logger.LogWarning("Десериализация замены \"{Title}\" вернула null", originalTitle);
                return null;
            }

            // Пост-обработка
            dto.Title = string.IsNullOrWhiteSpace(dto.Title) ? $"Альтернатива {originalTitle}" : dto.Title.Trim();
            dto.Description ??= string.Empty;
            dto.Instructions ??= new List<string>();
            dto.Ingredients ??= new List<GeneratedIngredientDto>();
            dto.MealType = NormalizeMealType(dto.MealType);

            if (dto.Calories <= 0)
                dto.Calories = targetCalories;

            _logger.LogDebug("Найдена замена: {Title} ({Calories} ккал) вместо {Original}",
                dto.Title, dto.Calories, originalTitle);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске замены для \"{Title}\"", originalTitle);
            return null;
        }
    }

    private sealed class TokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public long expires_at { get; set; }
    }

    private sealed class ResponseMessage
    {
        public List<Choice> choices { get; set; } = new();
        public sealed class Choice { public ChatMessage message { get; set; } = new(); }
        public sealed class ChatMessage { public string role { get; set; } = ""; public string content { get; set; } = ""; }
    }

    public sealed class GeneratedMealDto
    {
        public string MealType { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsSnack { get; set; }
        public int Calories { get; set; }
        public int Proteins { get; set; }
        public int Fats { get; set; }
        public int Carbs { get; set; }
        public List<string> Instructions { get; set; } = new();
        public List<GeneratedIngredientDto> Ingredients { get; set; } = new();
    }

    public sealed class GeneratedIngredientDto
    {
        public string Name { get; set; } = "";
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = "";
        public string Category { get; set; } = "";
        public double Calories { get; set; }
        public double Proteins { get; set; }
        public double Fats { get; set; }
        public double Carbs { get; set; }
    }
}

public sealed class GigaChatOptions
{
    public string ClientId { get; set; } = "";
    public string AuthKey { get; set; } = "";
}
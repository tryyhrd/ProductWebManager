using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProductWebManager.Services
{
    public class OpenFoodFactsProductDto
    {
        [JsonPropertyName("product_name")]
        public string? ProductName { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("nutriments")]
        public OpenFoodFactsNutriments? Nutriments { get; set; }
    }

    public class OpenFoodFactsNutriments
    {
        [JsonPropertyName("energy-kcal_100g")]
        public double? Calories100g { get; set; }

        [JsonPropertyName("proteins_100g")]
        public double? Proteins100g { get; set; }

        [JsonPropertyName("fat_100g")]
        public double? Fat100g { get; set; }

        [JsonPropertyName("carbohydrates_100g")]
        public double? Carbohydrates100g { get; set; }
    }

    public class OpenFoodFactsSearchResponse
    {
        [JsonPropertyName("products")]
        public List<OpenFoodFactsProductDto> Products { get; set; } = new();
    }

    public class OpenFoodFactsService
    {
        private readonly HttpClient _httpClient;

        public OpenFoodFactsService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://world.openfoodfacts.org/");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ProductWebManager/1.0 (Integration for local pantry app - contact: info@example.com)");
        }

        public async Task<List<OpenFoodFactsProductDto>> SearchProductsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<OpenFoodFactsProductDto>();

            query = query.Trim();

            // Try the original full query
            var results = await PerformSearchAsync(query);
            if (results.Any()) return results;

            // Fallback: try removing words from the right (e.g. "Молоко коровье" -> "Молоко")
            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            while (words.Length > 1)
            {
                words = words.Take(words.Length - 1).ToArray();
                var fallbackQuery = string.Join(" ", words);
                results = await PerformSearchAsync(fallbackQuery);
                if (results.Any()) return results;
            }

            return new List<OpenFoodFactsProductDto>();
        }

        private async Task<List<OpenFoodFactsProductDto>> PerformSearchAsync(string query)
        {
            int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    var response = await _httpClient.GetAsync($"cgi/search.pl?search_terms={Uri.EscapeDataString(query)}&search_simple=1&action=process&json=1&page_size=20");
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<OpenFoodFactsSearchResponse>(content);

                    // Filter out products with no name or no nutrition data
                    var items = result?.Products
                        .Where(p => !string.IsNullOrWhiteSpace(p.ProductName) && p.Nutriments != null && p.Nutriments.Calories100g.HasValue)
                        .Take(15) // Limit to top 15 results
                        .ToList();

                    if (items != null && items.Count > 0)
                    {
                        return items;
                    }

                    if (i < maxRetries - 1)
                    {
                        await Task.Delay(500); // Retry delay
                    }
                }
                catch (Exception ex)
                {
                    if (i == maxRetries - 1)
                    {
                        // In production, log the exception.
                        Console.WriteLine($"Error fetching from Open Food Facts: {ex.Message}");
                        return new List<OpenFoodFactsProductDto>();
                    }
                    await Task.Delay(500); // Retry delay
                }
            }
            return new List<OpenFoodFactsProductDto>();
        }
    }
}

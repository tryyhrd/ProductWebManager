namespace ProductWebManager.Classes.Helpers
{
    public static class ProductWeightDefaults
    {
        public static readonly Dictionary<string, double> Weights =
            new()
            {
                { "яйцо", 60 },
                { "банан", 120 },
                { "яблоко", 180 },
                { "помидор", 120 },
                { "огурец", 100 },
                { "картофель", 150 },
                { "лук", 80 },
                { "морковь", 90 },
                { "апельсин", 170 },
                { "мандарин", 70 },
                { "хлеб", 30 }
            };

        public static double GetWeight(string productName)
        {
            var key = productName.Trim().ToLower();

            if (Weights.TryGetValue(key, out var value))
                return value;

            return 100;
        }
    }
}
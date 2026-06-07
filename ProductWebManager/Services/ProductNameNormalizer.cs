namespace ProductWebManager.Services;

/// <summary>
/// Нормализация и нечёткое сравнение имён продуктов для предотвращения дубликатов в БД.
/// </summary>
public static class ProductNameNormalizer
{
    /// <summary>
    /// Нормализует имя продукта: обрезка пробелов, нижний регистр, замена «ё» на «е»,
    /// удаление двойных пробелов.
    /// </summary>
    public static string Normalize(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var result = name.Trim().ToLowerInvariant().Replace('ё', 'е');

        // Убираем двойные пробелы
        while (result.Contains("  "))
            result = result.Replace("  ", " ");

        return result;
    }

    /// <summary>
    /// Нечёткое сравнение: проверяет, являются ли два имени «одним и тем же продуктом».
    /// Возвращает true если:
    /// - одно полностью содержит другое (после нормализации), или
    /// - расстояние Левенштейна ≤ 2 символа при длине ≥ 5.
    /// </summary>
    public static bool AreSimilar(string a, string b)
    {
        var na = Normalize(a);
        var nb = Normalize(b);

        if (string.IsNullOrEmpty(na) || string.IsNullOrEmpty(nb))
            return false;

        // Точное совпадение
        if (na == nb)
            return true;

        // Одно содержит другое
        if (na.Contains(nb) || nb.Contains(na))
            return true;

        // Расстояние Левенштейна для коротких различий (опечатки AI)
        if (na.Length >= 5 && nb.Length >= 5)
        {
            int distance = LevenshteinDistance(na, nb);
            int maxLen = Math.Max(na.Length, nb.Length);
            // Допускаем ≤ 2 символа разницы или ≤ 15% длины
            if (distance <= 2 || (double)distance / maxLen <= 0.15)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Расстояние Левенштейна между двумя строками.
    /// </summary>
    public static int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;

        var d = new int[a.Length + 1, b.Length + 1];

        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[a.Length, b.Length];
    }
}

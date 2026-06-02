using System.Text.Json;

namespace PropertyManagement.MVC.Helpers
{
    public static class AmenitiesFormatter
    {
        // Amenities are stored as either a comma-separated string ("Parking, Pool")
        // or as a serialized JSON array (["Parking","Pool"]). Views render them
        // as comma-separated text; this helper hides that detail from the view.
        public static IReadOnlyList<string> Parse(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw == "[]") return Array.Empty<string>();

            var trimmed = raw.Trim();
            if (trimmed.StartsWith('['))
            {
                try
                {
                    var arr = JsonSerializer.Deserialize<List<string>>(trimmed);
                    if (arr != null) return arr;
                }
                catch (JsonException) { /* fall through to CSV parse */ }
            }

            return trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        public static string Format(string? raw) => string.Join(", ", Parse(raw));
    }
}

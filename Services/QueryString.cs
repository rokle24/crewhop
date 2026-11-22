namespace CrewHop.Portal.Services;

public static class QueryString
{
    public static string? Get(string absoluteUri, string key)
    {
        var uri = new Uri(absoluteUri);
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
        {
            return null;
        }

        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0)
            {
                continue;
            }

            var name = Uri.UnescapeDataString(parts[0]);
            if (!name.Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
        }

        return null;
    }
}

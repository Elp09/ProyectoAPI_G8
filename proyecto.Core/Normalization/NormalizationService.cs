using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using proyecto.Core.Schema;
using proyecto.Models;

namespace proyecto.Core.Normalization;

public class NormalizationService : INormalizationService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        WriteIndented = true,
    };

    public bool IsAlreadyNormalized(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var prop in doc.RootElement.EnumerateObject())
                if (prop.Name.Equals("schemaVersion", StringComparison.OrdinalIgnoreCase)
                    && prop.Value.GetString() == "edu.univ.ingest.v1")
                    return true;
            return false;
        }
        catch { return false; }
    }

    public IngestDocument? Deserialize(string json)
    {
        try { return JsonSerializer.Deserialize<IngestDocument>(json, _jsonOptions); }
        catch { return null; }
    }

    public List<IngestDocument> Normalize(string rawJson, Source source)
    {
        rawJson = rawJson?.Trim().TrimStart('\uFEFF') ?? string.Empty;

        // Si ya está normalizado, solo lo deserializamos
        if (IsAlreadyNormalized(rawJson))
        {
            var existing = Deserialize(rawJson);
            return existing is not null ? new List<IngestDocument> { existing } : new();
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJson);

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                return doc.RootElement.EnumerateArray()
                    .Select(el => NormalizeElement(el, source, rawJson))
                    .ToList();
            }

            return new List<IngestDocument> { NormalizeElement(doc.RootElement, source, rawJson) };
        }
        catch (JsonException)
        {
            // Contenido no JSON (HTML, texto plano, XML, etc.)
            return new List<IngestDocument> { CreateFallback(rawJson, source) };
        }
    }

    private IngestDocument NormalizeElement(JsonElement el, Source source, string rawJson)
    {
        var sourceSlug = Slugify(source.Name);
        var externalId = GetString(el, "id", "externalId", "external_id", "guid");
        var content    = GetString(el, "content", "body", "text", "description", "quote", "title") ?? string.Empty;
        var hash       = ShortHash(externalId ?? content);

        return new IngestDocument
        {
            ExportedAt = DateTime.UtcNow,
            Source = new IngestSource
            {
                Id             = sourceSlug,
                Name           = source.Name,
                Type           = source.ComponentType,
                Url            = source.Url,
                RequiresSecret = source.RequiresSecret,
            },
            Normalized = new IngestNormalized
            {
                Id         = $"{sourceSlug}:{hash}",
                ExternalId = externalId,
                Title      = GetString(el, "title", "headline", "name", "subject") ?? $"Contenido de {source.Name}",
                Content    = content,
                Summary    = GetString(el, "summary", "excerpt", "description"),
                PublishedAt = GetDateTime(el, "publishedAt", "published_at", "date", "pubDate", "created_at"),
                Url        = GetString(el, "url", "link", "href"),
                Author     = GetString(el, "author", "byline", "creator"),
                Language   = GetString(el, "language", "lang") ?? "es",
                Category   = new IngestCategory
                {
                    Primary   = GetString(el, "category", "section", "topic"),
                    Secondary = new List<string>(),
                },
            },
            Raw = new IngestRaw
            {
                Format = "json",
                Data   = new IngestRawData { Original = JsonSerializer.Deserialize<object>(el.GetRawText()) },
            },
        };
    }

    private IngestDocument CreateFallback(string rawContent, Source source)
    {
        var sourceSlug = Slugify(source.Name);
        var hash = ShortHash(rawContent);

        return new IngestDocument
        {
            ExportedAt = DateTime.UtcNow,
            Source = new IngestSource
            {
                Id             = sourceSlug,
                Name           = source.Name,
                Type           = source.ComponentType,
                Url            = source.Url,
                RequiresSecret = source.RequiresSecret,
            },
            Normalized = new IngestNormalized
            {
                Id      = $"{sourceSlug}:{hash}",
                Title   = $"Contenido de {source.Name}",
                Content = rawContent,
                Language = "es",
            },
            Raw = new IngestRaw
            {
                Format = "text",
                Data   = new IngestRawData { Original = rawContent },
            },
        };
    }

    // --- Helpers ---

    private static string? GetString(JsonElement el, params string[] candidates)
    {
        foreach (var key in candidates)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                var val = prop.GetString();
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
        }
        return null;
    }

    private static DateTime? GetDateTime(JsonElement el, params string[] candidates)
    {
        foreach (var key in candidates)
        {
            if (el.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String)
            {
                if (DateTime.TryParse(prop.GetString(), out var dt)) return dt;
            }
        }
        return null;
    }

    private static string Slugify(string name)
        => name.ToLowerInvariant().Replace(" ", "-").Replace("_", "-");

    private static string ShortHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }
}

namespace proyecto.Core.Schema;

/// <summary>
/// Raíz del esquema de normalización edu.univ.ingest.v1 acordado entre grupos.
/// </summary>
public class IngestDocument
{
    public string SchemaVersion { get; set; } = "edu.univ.ingest.v1";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public IngestSource Source { get; set; } = new();
    public IngestNormalized Normalized { get; set; } = new();
    public IngestRaw Raw { get; set; } = new();
}

public class IngestSource
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "api";
    public string Url { get; set; } = string.Empty;
    public bool RequiresSecret { get; set; }
}

public class IngestNormalized
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Url { get; set; }
    public string? Author { get; set; }
    public string Language { get; set; } = "es";
    public IngestCategory Category { get; set; } = new();
}

public class IngestCategory
{
    public string? Primary { get; set; }
    public List<string> Secondary { get; set; } = new();
}

public class IngestRaw
{
    public string Format { get; set; } = "json";
    public IngestRawData Data { get; set; } = new();
}

public class IngestRawData
{
    public object? Original { get; set; }
}

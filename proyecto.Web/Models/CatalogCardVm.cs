namespace proyecto.Web.Models;

public class CatalogCardVm
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<CardField> Fields { get; set; } = new();
}

public class CardField
{
    public string Label { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool IsSection { get; set; }
    public List<CardField> Children { get; set; } = new();
}

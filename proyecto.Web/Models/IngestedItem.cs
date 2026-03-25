namespace proyecto.Web.Models;

/// <summary>
/// Representa un ítem ingestado temporalmente (aún no guardado en BD).
/// </summary>
public class IngestedItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int SourceId { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string? Endpoint { get; set; }
    public bool IsLocalUpload { get; set; }
    public DateTime FetchedAt { get; set; } = DateTime.Now;
    public string Json { get; set; } = string.Empty;
}

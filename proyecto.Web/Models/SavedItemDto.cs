namespace proyecto.Web.Models;

/// <summary>
/// DTO para ítems ya guardados en BD, recibidos desde la API.
/// </summary>
public class SavedItemDto
{
    public int Id { get; set; }
    public int SourceId { get; set; }
    public string Json { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Endpoint { get; set; }
    public bool IsLocalUpload { get; set; }
    public string? SavedBy { get; set; }
}

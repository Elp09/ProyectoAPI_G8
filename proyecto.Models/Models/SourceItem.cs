namespace proyecto.Models;

public partial class SourceItem
{
    public int Id { get; set; }

    public int SourceId { get; set; }

    public string Json { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? Endpoint { get; set; }

    public bool IsLocalUpload { get; set; }

    public string? SavedBy { get; set; }

    public virtual Source Source { get; set; } = null!;
}

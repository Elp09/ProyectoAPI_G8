namespace proyecto.Models;

public partial class Source
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? Description { get; set; }

    public string ComponentType { get; set; } = null!;

    public bool RequiresSecret { get; set; }

    public string AuthType { get; set; } = null!;

    public string? Endpoint { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Secret> Secrets { get; set; } = new List<Secret>();

    public virtual ICollection<SourceItem> SourceItems { get; set; } = new List<SourceItem>();
}

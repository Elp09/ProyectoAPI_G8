namespace proyecto.Models;

public partial class Secret
{
    public int Id { get; set; }

    public int? SourceId { get; set; }

    public string KeyName { get; set; } = null!;

    public string KeyValue { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Source? Source { get; set; }
}

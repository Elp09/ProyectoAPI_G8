namespace proyecto.Web.Models;

public class IngestedItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SourceName { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public bool IsLocalUpload { get; set; } = false;
    public DateTime FetchedAt { get; set; }
    public string Json { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

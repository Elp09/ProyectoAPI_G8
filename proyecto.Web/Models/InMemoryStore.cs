namespace proyecto.Web.Models;

public class InMemoryStore
{
    public List<ApiSource> Sources { get; } = new();
    public List<IngestedItem> Items { get; } = new();
}

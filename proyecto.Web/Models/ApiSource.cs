namespace proyecto.Web.Models;

public class ApiSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ComponentType { get; set; } = "api";
    public bool RequiresSecret { get; set; }
    public string AuthType { get; set; } = "none";
    public string? Endpoint { get; set; }
}

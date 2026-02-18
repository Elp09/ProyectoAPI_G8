namespace proyecto.Web.Models;

public class ApiSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string AuthType { get; set; } = "none";
    public string? Secret { get; set; }
    public string? Endpoint { get; set; }
}

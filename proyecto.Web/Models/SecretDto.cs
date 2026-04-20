namespace proyecto.Web.Models;

public class SecretDto
{
    public int Id { get; set; }
    public int? SourceId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string KeyValue { get; set; } = string.Empty;
}

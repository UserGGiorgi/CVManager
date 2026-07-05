namespace CVManager.Core.Entities;

public class CVAttributeValue
{
    public int Id { get; set; }
    public int CVId { get; set; }
    public int AttributeId { get; set; }
    public string? Value { get; set; }

    // Navigation
    public CV CV { get; set; } = null!;
    public Attribute Attribute { get; set; } = null!;
}
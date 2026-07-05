namespace CVManager.Core.Entities;

public class PositionAttribute
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public int AttributeId { get; set; }
    public bool IsRequired { get; set; }
    public int Order { get; set; }

    // Navigation
    public Position Position { get; set; } = null!;
    public Attribute Attribute { get; set; } = null!;
}
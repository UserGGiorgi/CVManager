namespace CVManager.Core.Entities;

public class Attribute
{
    public int Id { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttributeType DataType { get; set; }
    public string? Options { get; set; } 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    // Navigation
    public User Creator { get; set; } = null!;
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public ICollection<ProfileAttribute> ProfileAttributes { get; set; } = new List<ProfileAttribute>();
}

public enum AttributeType
{
    String,
    Text,
    Image,
    Numeric,
    Date,
    Period,
    Boolean,
    OneOfMany
}
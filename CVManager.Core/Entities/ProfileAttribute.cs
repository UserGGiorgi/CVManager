namespace CVManager.Core.Entities;

public class ProfileAttribute
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public int AttributeId { get; set; }
    public string? Value { get; set; } // Stored as JSON string for complex types
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public Attribute Attribute { get; set; } = null!;
}
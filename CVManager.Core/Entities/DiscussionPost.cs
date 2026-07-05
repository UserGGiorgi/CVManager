namespace CVManager.Core.Entities;

public class DiscussionPost
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Guid AuthorId { get; set; }
    public string Content { get; set; } = string.Empty; 
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Position Position { get; set; } = null!;
    public User Author { get; set; } = null!;
}
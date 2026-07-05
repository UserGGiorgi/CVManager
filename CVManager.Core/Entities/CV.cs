namespace CVManager.Core.Entities;

public class CV
{
    public int Id { get; set; }
    public int PositionId { get; set; }
    public Guid CandidateId { get; set; }
    public CVStatus Status { get; set; } = CVStatus.Draft;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Position Position { get; set; } = null!;
    public User Candidate { get; set; } = null!;
    public ICollection<CVAttributeValue> AttributeValues { get; set; } = new List<CVAttributeValue>();
    public ICollection<CVLike> Likes { get; set; } = new List<CVLike>();
}

public enum CVStatus
{
    Draft,
    Published
}
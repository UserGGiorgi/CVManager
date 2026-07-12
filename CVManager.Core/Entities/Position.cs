namespace CVManager.Core.Entities;

public class Position
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Company { get; set; }
    public PositionLevel? Level { get; set; }
    public AccessType AccessType { get; set; } = AccessType.Public;
    public string? AccessRules { get; set; } // JSON
    public int MaxProjects { get; set; } = 5;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Guid CreatedBy { get; set; }

    // Navigation
    public User Creator { get; set; } = null!;
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = new List<PositionAttribute>();
    public List<string> ProjectTags { get; set; } = new List<string>();
    public ICollection<CV> CVs { get; set; } = new List<CV>();
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
}

public enum PositionLevel
{
    Junior,
    Middle,
    Senior,
    CLevel
}

public enum AccessType
{
    Public,
    Restricted
}
namespace CVManager.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } // Markdown
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string> Technologies { get; set; } = new List<string>();

    // Navigation
    public User User { get; set; } = null!;
}
namespace CVManager.Core.Entities;

public class CVLike
{
    public int CVId { get; set; }
    public Guid RecruiterId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public CV CV { get; set; } = null!;
    public User Recruiter { get; set; } = null!;
}
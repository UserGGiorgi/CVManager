using Microsoft.AspNetCore.Identity;

namespace CVManager.Core.Entities;

public class User : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProfileAttribute> ProfileAttributes { get; set; } = new List<ProfileAttribute>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<CV> CVs { get; set; } = new List<CV>();
    public ICollection<CVLike> GivenLikes { get; set; } = new List<CVLike>();
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = new List<DiscussionPost>();
}
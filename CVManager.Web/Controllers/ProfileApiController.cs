using CVManager.Core.Entities;
using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers.Api;

[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfileApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public ProfileApiController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private Guid UserId => User.GetUserId();

    // Update user fields (FirstName, LastName, Location)
    [HttpPut("field")]
    public async Task<IActionResult> UpdateField([FromBody] UpdateFieldDto dto)
    {
        var user = await _userManager.FindByIdAsync(UserId.ToString());
        if (user == null) return NotFound();

        switch (dto.FieldName?.ToLower())
        {
            case "firstname":
                user.FirstName = dto.Value ?? "";
                break;
            case "lastname":
                user.LastName = dto.Value ?? "";
                break;
            case "location":
                user.Location = dto.Value;
                break;
            default:
                return BadRequest("Invalid field name");
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    // Update or create profile attribute with optimistic locking
    [HttpPut("attribute")]
    public async Task<IActionResult> UpdateAttribute([FromBody] UpdateAttributeDto dto)
    {
        var profileAttr = await _db.ProfileAttributes
            .Include(pa => pa.Attribute)
            .FirstOrDefaultAsync(pa => pa.UserId == UserId && pa.AttributeId == dto.AttributeId);

        if (profileAttr == null)
        {
            // New attribute
            profileAttr = new ProfileAttribute
            {
                UserId = UserId,
                AttributeId = dto.AttributeId,
                Value = dto.Value,
                Version = Guid.NewGuid()
            };
            _db.ProfileAttributes.Add(profileAttr);
        }
        else
        {
            // Check version for optimistic concurrency
            if (profileAttr.Version != dto.Version)
                return Conflict("The record has been modified by another user. Please refresh.");

            profileAttr.Value = dto.Value;
            profileAttr.Version = Guid.NewGuid();
            profileAttr.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { version = profileAttr.Version });
    }

    [HttpDelete("attribute/{attributeId}")]
    public async Task<IActionResult> RemoveAttribute(int attributeId)
    {
        var profileAttr = await _db.ProfileAttributes
            .FirstOrDefaultAsync(pa => pa.UserId == UserId && pa.AttributeId == attributeId);

        if (profileAttr != null)
        {
            _db.ProfileAttributes.Remove(profileAttr);
            await _db.SaveChangesAsync();
        }
        return Ok();
    }

    // Project CRUD
    [HttpPost("project")]
    public async Task<IActionResult> AddProject([FromBody] ProjectDto dto)
    {
        var project = new Project
        {
            UserId = UserId,
            Name = dto.Name,
            Description = dto.Description,
            StartDate = dto.StartDate ?? DateTime.UtcNow,
            EndDate = dto.EndDate,
            Technologies = dto.Technologies ?? new List<string>()
        };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return Ok(new { project.Id });
    }

    [HttpPut("project/{id}")]
    public async Task<IActionResult> UpdateProject(int id, [FromBody] ProjectDto dto)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);
        if (project == null) return NotFound();

        project.Name = dto.Name;
        project.Description = dto.Description;
        project.StartDate = dto.StartDate ?? project.StartDate;
        project.EndDate = dto.EndDate;
        project.Technologies = dto.Technologies ?? project.Technologies;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("project/{id}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id && p.UserId == UserId);
        if (project != null)
        {
            _db.Projects.Remove(project);
            await _db.SaveChangesAsync();
        }
        return Ok();
    }
}

// DTOs
public class UpdateFieldDto
{
    public string FieldName { get; set; } = "";
    public string Value { get; set; } = "";
}

public class UpdateAttributeDto
{
    public int AttributeId { get; set; }
    public string? Value { get; set; }
    public Guid Version { get; set; }
}

public class ProjectDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<string>? Technologies { get; set; }
}
using CVManager.Core.Entities;
using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CVManager.Web.Controllers;

[Authorize]
public class CVController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public CVController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private Guid UserId => User.GetUserId();

    // GET: /CV – list of user's CVs
    public async Task<IActionResult> Index()
    {
        var cvs = await _db.CVs
            .Include(c => c.Position)
            .Include(c => c.AttributeValues)
            .ThenInclude(av => av.Attribute)
            .Where(c => c.CandidateId == UserId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        // Filter out CVs for positions the candidate no longer has access to
        var accessibleCvs = new List<CV>();
        foreach (var cv in cvs)
        {
            if (await CanAccessPosition(cv.Position))
                accessibleCvs.Add(cv);
        }

        return View(accessibleCvs);
    }

    // GET: /CV/Create – choose a position
    public async Task<IActionResult> Create()
    {
        var positions = await GetAccessiblePositions();
        return View(positions);
    }

    // POST: /CV/Create?positionId=5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int positionId)
    {
        var position = await _db.Positions
            .Include(p => p.PositionAttributes)
            .ThenInclude(pa => pa.Attribute)
            .FirstOrDefaultAsync(p => p.Id == positionId);

        if (position == null) return NotFound();

        if (!await CanAccessPosition(position))
            return Forbid();

        // Check if a CV already exists for this user+position
        var existing = await _db.CVs.FirstOrDefaultAsync(c => c.CandidateId == UserId && c.PositionId == positionId);
        if (existing != null)
        {
            // Redirect to edit existing CV
            return RedirectToAction("Edit", new { id = existing.Id });
        }

        // Create new CV
        var cv = new CV
        {
            PositionId = positionId,
            CandidateId = UserId,
            Status = CVStatus.Draft,
            Version = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.CVs.Add(cv);
        await _db.SaveChangesAsync();

        // Pre-fill attribute values from user's profile
        foreach (var pa in position.PositionAttributes)
        {
            var profileAttr = await _db.ProfileAttributes
                .FirstOrDefaultAsync(pr => pr.UserId == UserId && pr.AttributeId == pa.AttributeId);

            var cvAttrValue = new CVAttributeValue
            {
                CVId = cv.Id,
                AttributeId = pa.AttributeId,
                Value = profileAttr?.Value // may be null
            };
            _db.CVAttributeValues.Add(cvAttrValue);
        }
        await _db.SaveChangesAsync();

        return RedirectToAction("Edit", new { id = cv.Id });
    }

    // GET: /CV/Edit/5 (Candidate only)
    public async Task<IActionResult> Edit(int id)
    {

        var cv = await _db.CVs
            .Include(c => c.Position)
            .ThenInclude(p => p.PositionAttributes)
            .ThenInclude(pa => pa.Attribute)
            .Include(c => c.AttributeValues)
            .ThenInclude(av => av.Attribute)
            .Include(c => c.Candidate)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cv == null) return NotFound();

        if (!User.IsInRole("Admin") && cv.CandidateId != UserId)
            return Forbid();

        // Load user profile attributes for pre-fill
        var profileAttributes = await _db.ProfileAttributes
            .Include(pa => pa.Attribute)
            .Where(pa => pa.UserId == cv.CandidateId)
            .ToListAsync();

        // Load user projects filtered by position
        var projects = await _db.Projects
            .Where(p => p.UserId == cv.CandidateId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        // Filter by position's project tags if any
        if (cv.Position.ProjectTags.Any())
        {
            projects = projects
                .Where(p => p.Technologies.Any(t => cv.Position.ProjectTags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                .ToList();
        }

        // Limit number of projects
        projects = projects.Take(cv.Position.MaxProjects > 0 ? cv.Position.MaxProjects : 5).ToList();

        ViewBag.ProfileAttributes = profileAttributes;
        ViewBag.Projects = projects;
        ViewBag.IsReadOnly = false;

        return View("Edit", cv);
    }

    // GET: /CV/View/5 (Recruiters read-only)
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> View(int id)
    {
        var cv = await _db.CVs
            .Include(c => c.Position)
            .ThenInclude(p => p.PositionAttributes)
            .ThenInclude(pa => pa.Attribute)
            .Include(c => c.AttributeValues)
            .ThenInclude(av => av.Attribute)
            .Include(c => c.Candidate)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cv == null) return NotFound();

        // Recruiters can view only if published (or if admin)
        if (!User.IsInRole("Admin") && cv.Status != CVStatus.Published)
            return Forbid();

        var profileAttributes = await _db.ProfileAttributes
            .Include(pa => pa.Attribute)
            .Where(pa => pa.UserId == cv.CandidateId)
            .ToListAsync();

        ViewBag.ProfileAttributes = profileAttributes;
        ViewBag.IsReadOnly = true;

        return View("Edit", cv); // Same view, but readonly
    }

    // POST: /CV/EditAttribute (AJAX)
    [HttpPost]
    public async Task<IActionResult> EditAttribute([FromBody] EditAttributeDto dto)
    {
        var cv = await _db.CVs.Include(c => c.Position).FirstOrDefaultAsync(c => c.Id == dto.CVId);
        if (cv == null) return NotFound();
        if (!User.IsInRole("Admin") && cv.CandidateId != User.GetUserId())
            return Forbid();

        // Update or create CV attribute value
        var cvAttr = await _db.CVAttributeValues
            .FirstOrDefaultAsync(av =>
                av.CVId == dto.CVId &&
                av.AttributeId == dto.AttributeId);

        if (cvAttr == null)
        {
            cvAttr = new CVAttributeValue
            {
                CVId = dto.CVId,
                AttributeId = dto.AttributeId,
                Value = dto.Value
            };

            _db.CVAttributeValues.Add(cvAttr);
        }
        else
        {
            cvAttr.Value = dto.Value;
        }

        // Synchronize with profile attribute
        var profileAttr = await _db.ProfileAttributes
            .FirstOrDefaultAsync(pa =>
                pa.UserId == cv.CandidateId &&
                pa.AttributeId == dto.AttributeId);

        if (profileAttr == null)
        {
            profileAttr = new ProfileAttribute
            {
                UserId = cv.CandidateId,
                AttributeId = dto.AttributeId,
                Value = dto.Value,
                Version = Guid.NewGuid(),
                UpdatedAt = DateTime.UtcNow
            };

            _db.ProfileAttributes.Add(profileAttr);
        }
        else
        {
            profileAttr.Value = dto.Value;
            profileAttr.Version = Guid.NewGuid();
            profileAttr.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // POST: /CV/Publish/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var cv = await _db.CVs
            .Include(c => c.Position)
            .ThenInclude(p => p.PositionAttributes)
            .Include(c => c.AttributeValues)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cv == null) return NotFound();

        if (!User.IsInRole("Admin") && cv.CandidateId != UserId)
            return Forbid();

        // Check if all required attributes are filled
        foreach (var pa in cv.Position.PositionAttributes.Where(pa => pa.IsRequired))
        {
            var value = cv.AttributeValues.FirstOrDefault(av => av.AttributeId == pa.AttributeId)?.Value;
            if (string.IsNullOrWhiteSpace(value))
            {
                return BadRequest("All required attributes must be filled before publishing.");
            }
        }

        cv.Status = CVStatus.Published;
        cv.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return RedirectToAction("Edit", new { id = cv.Id });
    }

    // POST: /CV/Like/5
    [HttpPost]
    [Authorize(Roles = "Recruiter,Admin")]
    public async Task<IActionResult> Like(int id)
    {
        var cv = await _db.CVs.FindAsync(id);
        if (cv == null) return NotFound();

        var existingLike = await _db.CVLikes
            .FirstOrDefaultAsync(l => l.CVId == id && l.RecruiterId == UserId);

        if (existingLike != null)
        {
            // Unlike
            _db.CVLikes.Remove(existingLike);
        }
        else
        {
            _db.CVLikes.Add(new CVLike
            {
                CVId = id,
                RecruiterId = UserId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { likes = await _db.CVLikes.CountAsync(l => l.CVId == id) });
    }

    // Helper: Get positions accessible to the candidate
    private async Task<List<Position>> GetAccessiblePositions()
    {
        var all = await _db.Positions
            .Include(p => p.PositionAttributes)
            .ToListAsync();

        var accessible = new List<Position>();
        foreach (var pos in all)
        {
            if (await CanAccessPosition(pos))
                accessible.Add(pos);
        }
        return accessible;
    }

    // Helper: Check access rules
    private async Task<bool> CanAccessPosition(Position position)
    {
        if (position.AccessType == AccessType.Public)
            return true;

        if (string.IsNullOrWhiteSpace(position.AccessRules))
            return false; // Restricted but no rules -> no access

        // Parse rules
        var rules = JsonSerializer.Deserialize<List<AccessRule>>(position.AccessRules);
        if (rules == null || !rules.Any()) return false;

        // Get user's profile attributes
        var profileAttrs = await _db.ProfileAttributes
            .Include(pa => pa.Attribute)
            .Where(pa => pa.UserId == UserId)
            .ToListAsync();

        foreach (var rule in rules)
        {
            var profileAttr = profileAttrs.FirstOrDefault(pa =>
                pa.Attribute.Name.Equals(rule.Attribute, StringComparison.OrdinalIgnoreCase));
            if (profileAttr == null) return false; // attribute missing

            // Evaluate operator
            switch (rule.Operator)
            {
                case ">":
                    if (decimal.TryParse(profileAttr.Value, out var val) &&
                        decimal.TryParse(rule.Value, out var ruleVal))
                    {
                        if (val <= ruleVal) return false;
                    }
                    else return false;
                    break;
                case "<":
                    if (decimal.TryParse(profileAttr.Value, out var v1) &&
                        decimal.TryParse(rule.Value, out var v2))
                    {
                        if (v1 >= v2) return false;
                    }
                    else return false;
                    break;
                case "==":
                    if (!string.Equals(profileAttr.Value, rule.Value, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
                case "!=":
                    if (string.Equals(profileAttr.Value, rule.Value, StringComparison.OrdinalIgnoreCase))
                        return false;
                    break;
                case "checked":
                    if (rule.AttributeType == "Boolean")
                    {
                        if (bool.TryParse(profileAttr.Value, out var b) && !b) return false;
                    }
                    break;
                default:
                    return false;
            }
        }
        return true;
    }

    // Helper class for deserialising access rules
    public class AccessRule
    {
        public string Attribute { get; set; } = "";
        public string Operator { get; set; } = "";
        public string Value { get; set; } = "";
        public string AttributeType { get; set; } = "";
    }
    public class EditAttributeDto
    {
        public int CVId { get; set; }
        public int AttributeId { get; set; }
        public string? Value { get; set; }
    }
}
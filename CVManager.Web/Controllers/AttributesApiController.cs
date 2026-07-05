using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers.Api;

[Route("api/attributes")]
[ApiController]
[Authorize]
public class AttributesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AttributesApiController(ApplicationDbContext db) => _db = db;

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? category)
    {
        var query = _db.Attributes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(a => a.Name.Contains(q));
        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        var attrs = await query.OrderBy(a => a.Category).ThenBy(a => a.Name)
                               .Select(a => new {
                                   a.Id,
                                   a.Name,
                                   a.Category,
                                   a.DataType,
                                   a.Description
                               })
                               .ToListAsync();
        return Ok(attrs);
    }
}
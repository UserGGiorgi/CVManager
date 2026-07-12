using CVManager.Core.Entities;
using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

[Authorize(Roles = "Recruiter,Admin")]
public class PositionController : Controller
{
    private readonly ApplicationDbContext _db;

    public PositionController(ApplicationDbContext db) => _db = db;

    // GET: /Position
    public async Task<IActionResult> Index(string? search)
    {
        var query = _db.Positions
            .Include(p => p.PositionAttributes)
            .ThenInclude(pa => pa.Attribute)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Title.Contains(search) || p.Description!.Contains(search));

        var positions = await query.OrderByDescending(p => p.UpdatedAt).ToListAsync();
        return View(positions);
    }

    // GET: /Position/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.AllAttributes = await _db.Attributes.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync();
        return View(new Position());
    }

    // POST: /Position/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Position position, int[] selectedAttributeIds, bool[] requiredAttributes)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.AllAttributes = await _db.Attributes.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync();
            return View(position);
        }

        position.CreatedBy = User.GetUserId();
        position.CreatedAt = DateTime.UtcNow;
        position.UpdatedAt = DateTime.UtcNow;

        _db.Positions.Add(position);
        await _db.SaveChangesAsync();

        // Add selected attributes with order and required flag
        if (selectedAttributeIds != null)
        {
            for (int i = 0; i < selectedAttributeIds.Length; i++)
            {
                var attrId = selectedAttributeIds[i];
                var isRequired = requiredAttributes != null && i < requiredAttributes.Length && requiredAttributes[i];
                _db.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeId = attrId,
                    IsRequired = isRequired,
                    Order = i
                });
            }
            await _db.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /Position/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var position = await _db.Positions
            .Include(p => p.PositionAttributes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return NotFound();

        ViewBag.AllAttributes = await _db.Attributes.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync();
        return View(position);
    }

    // POST: /Position/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Position model, int[] selectedAttributeIds, bool[] requiredAttributes)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            ViewBag.AllAttributes = await _db.Attributes.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync();
            return View(model);
        }

        var position = await _db.Positions
            .Include(p => p.PositionAttributes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (position == null) return NotFound();

        position.Title = model.Title;
        position.Description = model.Description;
        position.Company = model.Company;
        position.Level = model.Level;
        position.AccessType = model.AccessType;
        position.AccessRules = model.AccessRules;
        position.MaxProjects = model.MaxProjects;
        position.UpdatedAt = DateTime.UtcNow;

        // Replace attributes
        _db.PositionAttributes.RemoveRange(position.PositionAttributes);
        await _db.SaveChangesAsync();

        if (selectedAttributeIds != null)
        {
            for (int i = 0; i < selectedAttributeIds.Length; i++)
            {
                var isRequired = requiredAttributes != null && i < requiredAttributes.Length && requiredAttributes[i];
                _db.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = position.Id,
                    AttributeId = selectedAttributeIds[i],
                    IsRequired = isRequired,
                    Order = i
                });
            }
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: /Position/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var position = await _db.Positions.FindAsync(id);
        if (position != null)
        {
            _db.Positions.Remove(position);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // Duplicate a position
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(int id)
    {
        var original = await _db.Positions
            .Include(p => p.PositionAttributes)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (original == null) return NotFound();

        var duplicate = new Position
        {
            Title = original.Title + " (Copy)",
            Description = original.Description,
            Company = original.Company,
            Level = original.Level,
            AccessType = original.AccessType,
            AccessRules = original.AccessRules,
            MaxProjects = original.MaxProjects,
            CreatedBy = User.GetUserId(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Positions.Add(duplicate);
        await _db.SaveChangesAsync();

        foreach (var pa in original.PositionAttributes)
        {
            _db.PositionAttributes.Add(new PositionAttribute
            {
                PositionId = duplicate.Id,
                AttributeId = pa.AttributeId,
                IsRequired = pa.IsRequired,
                Order = pa.Order
            });
        }
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMultiple(List<int> selectedIds)
    {
        if (selectedIds != null && selectedIds.Any())
        {
            var positions = await _db.Positions.Where(p => selectedIds.Contains(p.Id)).ToListAsync();
            _db.Positions.RemoveRange(positions);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuplicateMultiple(List<int> selectedIds)
    {
        if (selectedIds == null || !selectedIds.Any())
            return RedirectToAction(nameof(Index));

        foreach (var id in selectedIds)
        {
            var original = await _db.Positions
                .Include(p => p.PositionAttributes)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (original == null) continue;

            var duplicate = new Position
            {
                Title = original.Title + " (Copy)",
                Description = original.Description,
                Company = original.Company,
                Level = original.Level,
                AccessType = original.AccessType,
                AccessRules = original.AccessRules,
                MaxProjects = original.MaxProjects,
                ProjectTags = original.ProjectTags,
                CreatedBy = User.GetUserId(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Positions.Add(duplicate);
            await _db.SaveChangesAsync();

            foreach (var pa in original.PositionAttributes)
            {
                _db.PositionAttributes.Add(new PositionAttribute
                {
                    PositionId = duplicate.Id,
                    AttributeId = pa.AttributeId,
                    IsRequired = pa.IsRequired,
                    Order = pa.Order
                });
            }
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
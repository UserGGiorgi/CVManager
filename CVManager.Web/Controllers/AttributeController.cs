using CVManager.Core.Entities;
using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

[Authorize(Roles = "Recruiter,Admin")]
public class AttributeController : Controller
{
    private readonly ApplicationDbContext _db;

    public AttributeController(ApplicationDbContext db) => _db = db;

    // GET: /Attribute
    public async Task<IActionResult> Index(string? search, string? category)
    {
        var query = _db.Attributes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.Name.Contains(search) || a.Description!.Contains(search));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        var attributes = await query.OrderBy(a => a.Category).ThenBy(a => a.Name).ToListAsync();
        return View(attributes);
    }

    // GET: /Attribute/Create
    public IActionResult Create() => View();

    // POST: /Attribute/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CVManager.Core.Entities.Attribute model)
    {
        if (!ModelState.IsValid) return View(model);
        model.CreatedBy = User.GetUserId();
        model.CreatedAt = DateTime.UtcNow;
        model.UpdatedAt = DateTime.UtcNow;
        _db.Attributes.Add(model);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: /Attribute/Edit/5
    public async Task<IActionResult> Edit(int id)
    {
        var attr = await _db.Attributes.FindAsync(id);
        if (attr == null) return NotFound();
        return View(attr);
    }

    // POST: /Attribute/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CVManager.Core.Entities.Attribute model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        var attr = await _db.Attributes.FindAsync(id);
        if (attr == null) return NotFound();

        attr.Name = model.Name;
        attr.Category = model.Category;
        attr.Description = model.Description;
        attr.DataType = model.DataType;
        attr.Options = model.Options; // JSON for dropdowns
        attr.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // POST: /Attribute/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var attr = await _db.Attributes.FindAsync(id);
        if (attr != null)
        {
            _db.Attributes.Remove(attr);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteMultiple(List<int> selectedIds)
    {
        if (selectedIds != null && selectedIds.Any())
        {
            var attrs = await _db.Attributes.Where(a => selectedIds.Contains(a.Id)).ToListAsync();
            _db.Attributes.RemoveRange(attrs);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
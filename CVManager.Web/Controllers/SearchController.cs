using CVManager.Data;
using CVManager.Web.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

public class SearchController : Controller
{
    private readonly ApplicationDbContext _db;

    public SearchController(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return View(new SearchResultsViewModel());

        var queryLower = q.ToLower();

        // Search positions by title or description
        var positions = await _db.Positions
            .Where(p => p.Title.ToLower().Contains(queryLower) ||
                        p.Description.ToLower().Contains(queryLower))
            .Take(10)
            .ToListAsync();

        // Search CVs by attribute values (plain text match)
        var cvs = await _db.CVAttributeValues
            .Include(av => av.CV)
                .ThenInclude(c => c.Position)
            .Include(av => av.CV)
                .ThenInclude(c => c.Candidate)
            .Include(av => av.Attribute)
            .Where(av => av.Value != null && av.Value.ToLower().Contains(queryLower))
            .Select(av => av.CV)
            .Distinct()
            .Take(20)
            .ToListAsync();

        // Search projects (optional) - you can add more later

        var model = new SearchResultsViewModel
        {
            Query = q,
            Positions = positions,
            CVs = cvs
        };

        return View(model);
    }
}
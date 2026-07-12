using CVManager.Core.Entities;
using CVManager.Data;
using CVManager.Web.Models;
using CVManager.Web.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CVManager.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext db,
        UserManager<User> userManager)
    {
        _logger = logger;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var latestPositions = await _db.Positions
            .OrderByDescending(p => p.UpdatedAt)
            .Take(10)
            .ToListAsync();

        var popularPositions = await _db.Positions
            .OrderByDescending(p => p.CVs.Count)
            .Take(5)
            .ToListAsync();

        // Fetch all projects and flatten tags in memory (Technologies is JSON)
        var allProjects = await _db.Projects.ToListAsync();
        var tagCloud = allProjects
            .SelectMany(p => p.Technologies)
            .GroupBy(t => t)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Take(20)
            .ToList();

        // Count roles efficiently
        var candidateRoleId = await _db.Roles
            .Where(r => r.Name == "Candidate")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();
        var recruiterRoleId = await _db.Roles
            .Where(r => r.Name == "Recruiter")
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        var totalCandidates = await _db.UserRoles
            .CountAsync(ur => ur.RoleId == candidateRoleId);
        var totalRecruiters = await _db.UserRoles
            .CountAsync(ur => ur.RoleId == recruiterRoleId);

        var totalCVs = await _db.CVs.CountAsync();
        var newCVs24h = await _db.CVs.CountAsync(c => c.CreatedAt >= DateTime.UtcNow.AddHours(-24));

        var model = new HomeViewModel
        {
            LatestPositions = latestPositions,
            PopularPositions = popularPositions,
            TagCloud = tagCloud,
            TotalCVs = totalCVs,
            TotalCandidates = totalCandidates,
            TotalRecruiters = totalRecruiters,
            NewCVs24h = newCVs24h
        };

        return View(model);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
    public IActionResult Search(string q)
    {
        return RedirectToAction("Index", "Search", new { q });
    }
}
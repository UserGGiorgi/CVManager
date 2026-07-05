using CVManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CVManager.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<User> _userManager;
    private readonly ApplicationDbContext _db;

    public ProfileController(UserManager<User> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        // Load profile attributes with their definitions
        var profileAttributes = await _db.ProfileAttributes
            .Include(pa => pa.Attribute)
            .Where(pa => pa.UserId == user.Id)
            .OrderBy(pa => pa.Attribute.Category)
            .ThenBy(pa => pa.Attribute.Name)
            .ToListAsync();

        // Load projects
        var projects = await _db.Projects
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        // Pass everything as a view model (we can use ViewBag/ViewData for simplicity now)
        ViewBag.User = user;
        ViewBag.ProfileAttributes = profileAttributes;
        ViewBag.Projects = projects;

        return View();
    }
}

// Extension helper (add to a separate file later)
public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this System.Security.Claims.ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : Guid.Empty;
    }
}
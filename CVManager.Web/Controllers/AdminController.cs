using CVManager.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<User> _userManager;

    public AdminController(UserManager<User> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();
        // Load roles for each user
        var userRoles = new Dictionary<Guid, IList<string>>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userRoles[user.Id] = roles;
        }
        ViewBag.UserRoles = userRoles;
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
            await _userManager.DeleteAsync(user);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user != null)
        {
            // Use LockoutEnd to block/unblock (simple method)
            if (user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
                user.LockoutEnd = null; // unblock
            else
                user.LockoutEnd = DateTime.UtcNow.AddYears(100); // block indefinitely
            await _userManager.UpdateAsync(user);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(Guid userId, string role, bool assign)
    {
        if (string.IsNullOrWhiteSpace(role)) return RedirectToAction(nameof(Index));
        var validRoles = new[] { "Candidate", "Recruiter", "Admin" };
        if (!validRoles.Contains(role)) return RedirectToAction(nameof(Index));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        if (assign)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);
        }
        else
        {
            if (await _userManager.IsInRoleAsync(user, role))
                await _userManager.RemoveFromRoleAsync(user, role);
        }

        return RedirectToAction(nameof(Index));
    }
}
using CVManager.Core.Entities;
using CVManager.Data;
using CVManager.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CVManager.Web.Controllers;

[Authorize]
public class DiscussionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<DiscussionHub> _hubContext;

    public DiscussionController(ApplicationDbContext db, IHubContext<DiscussionHub> hubContext)
    {
        _db = db;
        _hubContext = hubContext;
    }

    // GET: /Discussion/Index?positionId=5
    public async Task<IActionResult> Index(int positionId)
    {
        var position = await _db.Positions.FindAsync(positionId);
        if (position == null) return NotFound();

        var posts = await _db.DiscussionPosts
            .Include(dp => dp.Author)
            .Where(dp => dp.PositionId == positionId)
            .OrderBy(dp => dp.CreatedAt)
            .ToListAsync();

        ViewBag.Position = position;
        return View(posts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPost(int positionId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest("Content cannot be empty.");

        var post = new DiscussionPost
        {
            PositionId = positionId,
            AuthorId = User.GetUserId(),
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
        _db.DiscussionPosts.Add(post);
        await _db.SaveChangesAsync();

        var author = await _db.Users.FindAsync(post.AuthorId);
        // Create a JSON object to send to clients
        var postData = new
        {
            authorName = author.FirstName + " " + author.LastName,
            authorId = author.Id,
            content = post.Content, // Markdown will be converted client-side
            createdAt = post.CreatedAt.ToString("g")
        };

        await _hubContext.Clients.Group($"position_{positionId}").SendAsync("NewPost", postData);

        return Ok();
    }

    private string RenderPartialViewToString(string viewName, object model)
    {
        // A simple helper to render a partial to a string; for real projects use a view renderer service.
        // We'll instead return JSON and let the client build the HTML.
        // For simplicity, we'll return JSON and build on client.
        return ""; // We'll adjust client to build HTML from JSON
    }
}
using System.Net;
using GraphOrgViewer.Models;
using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class ObsController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public ObsController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index(int depth = 10)
    {

        var tree = await _graphOrgService.GetOrganizationTreeAsync();

        var root = tree
            .OrderByDescending(x => x.Children.Count)
            .FirstOrDefault();

        if (root == null)
        {
            return View(null);
        }

        ViewBag.Depth = depth;

        var model = MapToTreantNode(root, depth:10);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Photo(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var photoStream = await _graphOrgService.GetUserPhotoStreamAsync(id);

        if (photoStream == null)
            return NotFound();

        return File(photoStream, "image/jpeg");
    }

    private TreantNodeViewModel MapToTreantNode(GraphUserViewModel user, int depth)
    {
        var initials = GetInitials(user.DisplayName);

        var photoUrl = !string.IsNullOrWhiteSpace(user.Id)
            ? Url.Action("Photo", "Obs", new { id = user.Id })
            : null;

        var innerHtml = $@"
            <div class=""obs-card"">
                <div class=""obs-avatar"">
                    <img src=""{photoUrl}"" alt="""" onerror=""this.remove();"" />
                    <span>{WebUtility.HtmlEncode(initials)}</span>
                </div>
                <div class=""obs-name"">{WebUtility.HtmlEncode(user.DisplayName)}</div>
                <div class=""obs-title"">{WebUtility.HtmlEncode(user.JobTitle)}</div>
                <div class=""obs-department"">{WebUtility.HtmlEncode(user.Department)}</div>
                <div class=""obs-email"">{WebUtility.HtmlEncode(user.UserPrincipalName)}</div>           
            </div>";

        var node = new TreantNodeViewModel
        {
            InnerHTML = innerHtml,
            HtmlClass = "obs-node"
        };

        if (depth <= 1)
        {
            return node;
        }

        node.Children = user.Children
            .OrderBy(x => x.DisplayName)
            .Select(child => MapToTreantNode(child, depth - 1))
            .ToList();

        return node;
    }

    private static string GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "?";

        return string.Join("", name
            .Split(" ", StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(x => x[0]));
    }
}
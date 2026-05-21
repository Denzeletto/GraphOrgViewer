using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class GroupsController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public GroupsController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index()
    {
        var model = await _graphOrgService.GetGroupsWithOwnersAsync();
        return View(model);
    }
}
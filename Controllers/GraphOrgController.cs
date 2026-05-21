using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class OrgController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public OrgController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index()
    {
        var tree = await _graphOrgService.GetOrganizationTreeAsync();
        return View(tree);
    }
}
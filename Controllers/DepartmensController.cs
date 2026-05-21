using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class DepartmentsController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public DepartmentsController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _graphOrgService.GetAllUsersAsync();

        var departments = users
            .GroupBy(u => string.IsNullOrWhiteSpace(u.Department)
                ? "Bez działu"
                : u.Department.Trim())
            .OrderBy(g => g.Key == "Bez działu" ? 1 : 0)
            .ThenBy(g => g.Key)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(u => u.DisplayName).ToList());

        return View(departments);
    }
}
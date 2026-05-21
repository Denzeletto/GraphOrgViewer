using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;
using GraphOrgViewer.Models;

namespace GraphOrgViewer.Controllers;

public class QuestsController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public QuestsController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index(string? userId)
    {
        var users = await _graphOrgService.GetUsersAsync();

        if (string.IsNullOrWhiteSpace(userId) && users.Any())
            userId = users.First().Id;

        var model = string.IsNullOrWhiteSpace(userId)
            ? new QuestViewModel()
            : await _graphOrgService.GetQuestDataAsync(userId);

        ViewBag.Users = users;
        ViewBag.SelectedUserId = userId;

        return View(model);
    }
}
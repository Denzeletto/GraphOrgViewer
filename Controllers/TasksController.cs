using GraphOrgViewer.Models;
using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class TasksController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    private readonly PlannerTaskService _plannerTaskService;

    public TasksController(
        GraphOrgService graphOrgService,
        PlannerTaskService plannerTaskService)
    {
        _graphOrgService = graphOrgService;
        _plannerTaskService = plannerTaskService;
    }

    public async Task<IActionResult> Department(
        string departmentName = "IT",
        List<string>? excludedUserIds = null)
    {
        excludedUserIds ??= new List<string>();

        var allUsers = await _graphOrgService.GetAllUsersAsync();

        var departmentUsers = allUsers
            .Where(u =>
                !string.IsNullOrWhiteSpace(u.Department) &&
                u.Department.Trim().Equals(departmentName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(u => u.DisplayName)
            .ToList();

        var includedUsers = departmentUsers
            .Where(u => !excludedUserIds.Contains(u.Id ?? ""))
            .ToList();

        var model = new DepartmentTasksViewModel
        {
            DepartmentName = departmentName,
            Users = departmentUsers,
            ExcludedUserIds = excludedUserIds
        };

        foreach (var user in includedUsers)
        {
            if (string.IsNullOrWhiteSpace(user.Id))
                continue;

            var questData = await _graphOrgService.GetQuestDataAsync(user.Id);

            model.UserTasks.Add(new UserTasksViewModel
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Tasks = questData.Tasks
            });
        }

        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> UpdateTask(
        string taskId,
        int priority,
        List<string> assignedUserIds,
        string departmentName = "IT")
    {
        await _plannerTaskService.UpdateTaskAsync(
            taskId,
            priority,
            assignedUserIds ?? new List<string>());

        return RedirectToAction(nameof(Department), new
        {
            departmentName
        });
    }
}
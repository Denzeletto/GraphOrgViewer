namespace GraphOrgViewer.Models;

public class DepartmentTasksViewModel
{
    public string DepartmentName { get; set; } = "";
    public List<GraphUserViewModel> Users { get; set; } = new();
    public List<string> ExcludedUserIds { get; set; } = new();
    public List<UserTasksViewModel> UserTasks { get; set; } = new();
}

public class UserTasksViewModel
{
    public string UserId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public List<PlannerTaskViewModel> Tasks { get; set; } = new();
}
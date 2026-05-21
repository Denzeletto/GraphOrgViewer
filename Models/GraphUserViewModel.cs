namespace GraphOrgViewer.Models;

public class GraphUserViewModel
{
    public string? Id { get; set; }
    public string? DisplayName { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? Mail { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? MobilePhone { get; set; }
    public string? BusinessPhone { get; set; }

    public string? ManagerId { get; set; }

    public List<GraphUserViewModel> Children { get; set; } = [];
}
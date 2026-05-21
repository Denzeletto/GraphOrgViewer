namespace GraphOrgViewer.Models;

public class GroupOwnerViewModel
{
    public string GroupId { get; set; } = "";
    public string GroupName { get; set; } = "";
    public string? Mail { get; set; }
    public string GroupType { get; set; } = "";
    public List<GroupOwnerPersonViewModel> Owners { get; set; } = new();
}

public class GroupOwnerPersonViewModel
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Mail { get; set; }
    public string? UserPrincipalName { get; set; }
}
namespace GraphOrgViewer.Models;

public class QuestViewModel
{
    public List<TeamChannelsViewModel> Teams { get; set; } = new();
    public List<PlannerTaskViewModel> Tasks { get; set; } = new();
}

public class TeamChannelsViewModel
{
    public string TeamId { get; set; } = "";
    public string TeamName { get; set; } = "";
    public List<ChannelViewModel> Channels { get; set; } = new();
}

public class ChannelViewModel
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? Description { get; set; }
}

public class PlannerTaskViewModel
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string? PlanId { get; set; }
    public string? BucketId { get; set; }
    public DateTimeOffset? DueDateTime { get; set; }
    public int? PercentComplete { get; set; }
    public string? PlanTitle { get; set; }
    public int? Priority { get; set; }
    public List<string> AssignedUserIds { get; set; } = new();

}
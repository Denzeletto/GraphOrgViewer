using Azure.Identity;
using GraphOrgViewer.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Serialization;

namespace GraphOrgViewer.Services;

public class GraphOrgService
{
    private readonly GraphServiceClient _graphClient;
    private readonly IConfiguration _configuration;

    public GraphOrgService(IConfiguration configuration)
    {
        var tenantId = configuration["Graph:TenantId"];
        var clientId = configuration["Graph:ClientId"];
        var clientSecret = configuration["Graph:ClientSecret"];
        _configuration = configuration;
        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("Brakuje konfiguracji Graph w appsettings.json.");
        }

        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret);

        _graphClient = new GraphServiceClient(
            credential,
            new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task<List<GraphUserViewModel>> GetOrganizationTreeAsync()
    {
        var response = await _graphClient.Users.GetAsync(request =>
        {
            request.QueryParameters.Select =
            [
                "id",
                "displayName",
                "userPrincipalName",
                "mail",
                "jobTitle",
                "department",
                "mobilePhone",
                "businessPhones",
                "assignedLicenses",
                "userType",
                "accountEnabled"
            ];

            request.QueryParameters.Expand =
            [
                "manager($select=id,displayName)"
            ];

            request.QueryParameters.Top = 999;
        });

        var users = response?.Value?
            .Where(u =>
                u.AccountEnabled == true &&
                u.UserType == "Member" &&
                u.AssignedLicenses?.Any() == true &&
                !string.IsNullOrWhiteSpace(u.DisplayName))
            .Select(u => new GraphUserViewModel
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                UserPrincipalName = u.UserPrincipalName,
                Mail = u.Mail,
                JobTitle = u.JobTitle,
                Department = u.Department,
                MobilePhone = u.MobilePhone,
                BusinessPhone = u.BusinessPhones?.FirstOrDefault(),
                ManagerId = u.Manager?.Id
            })
            .ToList() ?? [];

        var usersById = users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .ToDictionary(u => u.Id!);

        var roots = new List<GraphUserViewModel>();

        foreach (var user in users)
        {
            if (!string.IsNullOrWhiteSpace(user.ManagerId)
                && usersById.TryGetValue(user.ManagerId, out var manager))
            {
                manager.Children.Add(user);
            }
            else
            {
                roots.Add(user);
            }
        }

        return roots
            .OrderBy(x => x.DisplayName)
            .ToList();
    }

    public async Task<List<GraphUserViewModel>> GetAllUsersAsync()
    {
        var response = await _graphClient.Users.GetAsync(request =>
        {
            request.QueryParameters.Select =
            [
                "id",
                "displayName",
                "userPrincipalName",
                "mail",
                "jobTitle",
                "department",
                "mobilePhone",
                "businessPhones",
                "assignedLicenses",
                "userType",
                "accountEnabled"
            ];

            request.QueryParameters.Top = 999;
            request.QueryParameters.Orderby = ["displayName"];
        });

        return response?.Value?
            .Where(u =>
                u.AccountEnabled == true &&
                u.UserType == "Member" &&
                u.AssignedLicenses?.Any() == true &&
                !string.IsNullOrWhiteSpace(u.DisplayName))
            .Select(u => new GraphUserViewModel
            {
                Id = u.Id,
                DisplayName = u.DisplayName,
                UserPrincipalName = u.UserPrincipalName,
                Mail = u.Mail,
                JobTitle = u.JobTitle,
                Department = u.Department,
                MobilePhone = u.MobilePhone,
                BusinessPhone = u.BusinessPhones?.FirstOrDefault()
            })
            .ToList() ?? [];
    }
    
    public async Task<Stream?> GetUserPhotoStreamAsync(string userId)
    {
        try
        {
            return await _graphClient.Users[userId].Photo.Content.GetAsync();
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuestViewModel> GetQuestDataAsync(string userId)
    {
        var result = new QuestViewModel();

        var teams = await _graphClient.Users[userId].JoinedTeams.GetAsync();

        if (teams?.Value != null)
        {
            foreach (var team in teams.Value)
            {
                var teamVm = new TeamChannelsViewModel
                {
                    TeamId = team.Id ?? "",
                    TeamName = team.DisplayName ?? "(bez nazwy)"
                };

                var channels = await _graphClient.Teams[team.Id].Channels.GetAsync();

                if (channels?.Value != null)
                {
                    teamVm.Channels = channels.Value.Select(c => new ChannelViewModel
                    {
                        Id = c.Id ?? "",
                        DisplayName = c.DisplayName ?? "(bez nazwy)",
                        Description = c.Description
                    }).ToList();
                }

                result.Teams.Add(teamVm);
            }
        }

        try
        {
            var tasks = await _graphClient.Users[userId].Planner.Tasks.GetAsync();

            if (tasks?.Value != null)
            {
                var planNames = new Dictionary<string, string>();

                foreach (var task in tasks.Value)
                {
                    var planTitle = task.PlanId;

                    if (!string.IsNullOrWhiteSpace(task.PlanId))
                    {
                        if (!planNames.TryGetValue(task.PlanId, out planTitle))
                        {
                            try
                            {
                                var plan = await _graphClient.Planner.Plans[task.PlanId].GetAsync();
                                planTitle = plan?.Title ?? task.PlanId;
                            }
                            catch
                            {
                                planTitle = task.PlanId;
                            }

                            planNames[task.PlanId] = planTitle;
                        }
                    }

                    var checklistItems = new List<PlannerChecklistItemViewModel>();
                    var description = "";

                    try
                    {
                        if (!string.IsNullOrWhiteSpace(task.Id))
                        {
                            var details = await _graphClient.Planner.Tasks[task.Id].Details.GetAsync();

                            description = details?.Description ?? "";
                            
                            if (details?.Checklist?.AdditionalData != null)
                            {
                                foreach (var item in details.Checklist.AdditionalData)
                                {
                                    if (item.Value is not UntypedObject checklistObject)
                                        continue;

                                    var title = "";
                                    var isChecked = false;

                                    var properties = checklistObject.GetValue();

                                    if (properties.TryGetValue("title", out var titleValue)
                                        && titleValue is UntypedString titleString)
                                    {
                                        title = titleString.GetValue();
                                    }

                                    if (properties.TryGetValue("isChecked", out var checkedValue)
                                        && checkedValue is UntypedBoolean checkedBoolean)
                                    {
                                        isChecked = checkedBoolean.GetValue();
                                    }

                                    checklistItems.Add(new PlannerChecklistItemViewModel
                                    {
                                        Id = item.Key,
                                        Title = title,
                                        IsChecked = isChecked
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Checklist parse error for task {task.Title}: {ex.Message}");
                    }

                    result.Tasks.Add(new PlannerTaskViewModel
                    {
                        Id = task.Id ?? "",
                        Title = task.Title ?? "(bez tytułu)",
                        PlanId = task.PlanId,
                        PlanTitle = planTitle,
                        BucketId = task.BucketId,
                        DueDateTime = task.DueDateTime,
                        PercentComplete = task.PercentComplete,
                        Priority = task.Priority,
                        AssignedUserIds = task.Assignments?.AdditionalData?.Keys.ToList() ?? new List<string>(),
                        ChecklistItems = checklistItems,
                        Description = description
                    });
                }
            }
        }
        catch
        {
        }

        return result;
    }

    public async Task<List<GraphUserViewModel>> GetUsersAsync()
    {
        var users = await _graphClient.Users.GetAsync(request =>
        {
            request.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "userPrincipalName",
                "mail"
            };

            request.QueryParameters.Top = 999;
        });

        return users?.Value?
            .Select(u => new GraphUserViewModel
            {
                Id = u.Id ?? "",
                DisplayName = u.DisplayName ?? u.UserPrincipalName ?? u.Mail ?? "(bez nazwy)",
                Mail = u.Mail,
                UserPrincipalName = u.UserPrincipalName
            })
            .OrderBy(u => u.DisplayName)
            .ToList() ?? new List<GraphUserViewModel>();
    }

    public async Task<List<GroupOwnerViewModel>> GetGroupsWithOwnersAsync()
    {
        var result = new List<GroupOwnerViewModel>();

        var groupsResponse = await _graphClient.Groups.GetAsync(request =>
        {
            request.QueryParameters.Select = new[]
            {
                "id",
                "displayName",
                "mail",
                "groupTypes",
                "mailEnabled",
                "securityEnabled"
            };

            request.QueryParameters.Top = 999;
        });

        if (groupsResponse?.Value == null)
            return result;

        foreach (var group in groupsResponse.Value)
        {
            var groupVm = new GroupOwnerViewModel
            {
                GroupId = group.Id ?? "",
                GroupName = group.DisplayName ?? "(bez nazwy)",
                Mail = group.Mail,
                GroupType = GetGroupTypeLabel(group)
            };

            try
            {
                var ownersResponse = await _graphClient.Groups[group.Id].Owners.GetAsync();

                if (ownersResponse?.Value != null)
                {
                    foreach (var owner in ownersResponse.Value)
                    {
                        if (owner is User user)
                        {
                            groupVm.Owners.Add(new GroupOwnerPersonViewModel
                            {
                                Id = user.Id ?? "",
                                DisplayName = user.DisplayName ?? "(bez nazwy)",
                                Mail = user.Mail,
                                UserPrincipalName = user.UserPrincipalName
                            });
                        }
                        else
                        {
                            groupVm.Owners.Add(new GroupOwnerPersonViewModel
                            {
                                Id = owner.Id ?? "",
                                DisplayName = owner.Id ?? "(inny typ właściciela)"
                            });
                        }
                    }
                }
            }
            catch
            {
                // Niektóre typy grup mogą nie zwracać właścicieli.
                // Nie blokujemy całej listy.
            }

            result.Add(groupVm);
        }

        return result.OrderBy(x => x.GroupName).ToList();
    }

    private static string GetGroupTypeLabel(Group group)
    {
        var isUnified = group.GroupTypes != null && group.GroupTypes.Contains("Unified");

        if (isUnified)
            return "Microsoft 365";

        if (group.SecurityEnabled == true && group.MailEnabled == false)
            return "Security";

        if (group.SecurityEnabled == true && group.MailEnabled == true)
            return "Mail-enabled security";

        if (group.SecurityEnabled == false && group.MailEnabled == true)
            return "Distribution";

        return "Inna";
    }
    public async Task<List<GraphUserViewModel>> GetUsersByDepartmentAsync(string departmentName)
    {
        var users = await GetUsersAsync();

        return users
            .Where(u =>
                !string.IsNullOrWhiteSpace(u.Department)
                && u.Department.Trim().Contains(departmentName, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<Dictionary<string, List<GraphUserViewModel>>> GetDepartmentsAsync()
    {
        var users = await GetUsersAsync();

        return users
            .Where(u => !string.IsNullOrWhiteSpace(u.JobTitle))
            .GroupBy(u => ResolveDepartmentName(u.JobTitle!))
            .Where(g => !string.IsNullOrWhiteSpace(g.Key))
            .ToDictionary(
                g => g.Key!,
                g => g.OrderBy(u => u.DisplayName).ToList()
            );
    }

    private static string? ResolveDepartmentName(string jobTitle)
    {
        if (jobTitle.Contains("IT", StringComparison.OrdinalIgnoreCase))
            return "IT";

        if (jobTitle.Contains("Handlow", StringComparison.OrdinalIgnoreCase))
            return "Handlowy";

        if (jobTitle.Contains("Serwis", StringComparison.OrdinalIgnoreCase))
            return "Serwis";

        return null;
    }

    public async Task<List<RoomBusySlotViewModel>> GetRoomCalendarAsync(
    string roomEmail,
    DateTime start,
    DateTime end)
    {
        var events = await _graphClient.Users[roomEmail].Calendar.CalendarView.GetAsync(request =>
        {
            request.QueryParameters.StartDateTime = start.ToString("o");
            request.Headers.Add(
                "Prefer",
                "outlook.timezone=\"Central European Standard Time\"");
            request.QueryParameters.EndDateTime = end.ToString("o");
            request.QueryParameters.Select =
            [
                "subject",
                "start",
                "end",
                "showAs",
                "organizer"
            ];
            request.QueryParameters.Orderby = ["start/dateTime"];
        });

        return events?.Value?.Select(e => new RoomBusySlotViewModel
        {
            Subject = e.Subject,
            Start = e.Start?.DateTime,
            End = e.End?.DateTime,
            Status = e.ShowAs?.ToString()
        }).ToList() ?? [];
    }
    public List<RoomViewModel> GetConfiguredRooms()
    {
        return _configuration
            .GetSection("Rooms")
            .Get<List<RoomViewModel>>() ?? [];
    }

    public async Task<List<RoomCalendarEventViewModel>> GetRoomCalendarAsync(
    string roomEmail,
    DateTime date)
    {
        var start = date.Date;
        var end = date.Date.AddDays(1);

        var events = await _graphClient.Users[roomEmail].Calendar.CalendarView.GetAsync(request =>
        {
            request.QueryParameters.StartDateTime = start.ToString("o");
            request.QueryParameters.EndDateTime = end.ToString("o");
            request.Headers.Add(
                "Prefer",
                "outlook.timezone=\"Central European Standard Time\"");
            request.QueryParameters.Select =
            [
                "subject",
                "start",
                "end",
                "showAs",
                "organizer"
            ];

            request.QueryParameters.Orderby = ["start/dateTime"];
        });

        return events?.Value?.Select(e => new RoomCalendarEventViewModel
        {
            Subject = e.Subject ?? "(zajęte)",
            Organizer = e.Organizer?.EmailAddress?.Name ?? "",
            Start = DateTime.Parse(e.Start?.DateTime ?? start.ToString("o")),
            End = DateTime.Parse(e.End?.DateTime ?? end.ToString("o")),
            ShowAs = e.ShowAs?.ToString() ?? ""
        }).ToList() ?? [];
    }

    public async Task BookRoomAsync(
    string organizerEmail,
    string roomEmail,
    DateTime start,
    DateTime end,
    string subject)
    {
        var newEvent = new Event
        {
            Subject = subject,
            Start = new DateTimeTimeZone
            {
                DateTime = start.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "Central European Standard Time"
            },
            End = new DateTimeTimeZone
            {
                DateTime = end.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "Central European Standard Time"
            },
            Location = new Location
            {
                DisplayName = roomEmail
            },
            Attendees =
            [
                new Attendee
                {
                    Type = AttendeeType.Resource,
                    EmailAddress = new EmailAddress
                    {
                        Address = roomEmail
                    }
                }
            ]
        };

        await _graphClient.Users[organizerEmail]
            .Events
            .PostAsync(newEvent);
    }
}
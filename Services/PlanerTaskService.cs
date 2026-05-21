using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace GraphOrgViewer.Services;

public class PlannerTaskService
{
    private readonly GraphServiceClient _graphClient;

    public PlannerTaskService(IConfiguration configuration)
    {
        var credential = new ClientSecretCredential(
            configuration["Graph:TenantId"],
            configuration["Graph:ClientId"],
            configuration["Graph:ClientSecret"]);

        _graphClient = new GraphServiceClient(
            credential,
            new[] { "https://graph.microsoft.com/.default" });
    }

    public async Task UpdateTaskAsync(
        string taskId,
        int priority,
        List<string> assignedUserIds)
    {
        var currentTask = await _graphClient.Planner.Tasks[taskId].GetAsync();

        if (currentTask == null)
            throw new InvalidOperationException("Nie znaleziono zadania.");

        var etag = currentTask.AdditionalData["@odata.etag"]?.ToString();

        if (string.IsNullOrWhiteSpace(etag))
            throw new InvalidOperationException("Brak ETag zadania.");

        var assignmentsData = new Dictionary<string, object>();

        if (currentTask.Assignments?.AdditionalData != null)
        {
            foreach (var existingAssignment in currentTask.Assignments.AdditionalData.Keys)
            {
                assignmentsData[existingAssignment] = null!;
            }
        }

        foreach (var userId in assignedUserIds.Distinct())
        {
            assignmentsData[userId] = new PlannerAssignment
            {
                OdataType = "#microsoft.graph.plannerAssignment",
                OrderHint = " !"
            };
        }

        var requestBody = new PlannerTask
        {
            Priority = priority,
            Assignments = new PlannerAssignments
            {
                AdditionalData = assignmentsData
            }
        };

        await _graphClient.Planner.Tasks[taskId].PatchAsync(
            requestBody,
            config =>
            {
                config.Headers.Add("If-Match", etag);
                config.Headers.Add("Prefer", "return=representation");
            });
    }
}
using GraphOrgViewer.Models;
using GraphOrgViewer.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphOrgViewer.Controllers;

public class RoomsController : Controller
{
    private readonly GraphOrgService _graphOrgService;

    public RoomsController(GraphOrgService graphOrgService)
    {
        _graphOrgService = graphOrgService;
    }

    public async Task<IActionResult> Index(string? roomEmail, DateTime? date)
    {
        var rooms = _graphOrgService.GetConfiguredRooms();

        var selectedDate = date ?? DateTime.Today;
        var selectedRoom = roomEmail ?? rooms.FirstOrDefault()?.Email;

        var events = new List<RoomCalendarEventViewModel>();

        if (!string.IsNullOrWhiteSpace(selectedRoom))
        {
            events = await _graphOrgService.GetRoomCalendarAsync(selectedRoom, selectedDate);
        }

        var model = new RoomCalendarPageViewModel
        {
            Rooms = rooms,
            SelectedRoomEmail = selectedRoom,
            SelectedDate = selectedDate,
            Events = events
        };

        return View(model);
    }
}
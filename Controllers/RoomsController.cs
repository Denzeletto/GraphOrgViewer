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
        var selectedDate = DateTime.Today;

        var events = new List<RoomCalendarEventViewModel>();
        var users = new List<GraphUserViewModel>();

        if (!string.IsNullOrWhiteSpace(roomEmail))
        {
            events = await _graphOrgService.GetRoomCalendarAsync(roomEmail, selectedDate);
            users = await _graphOrgService.GetAllUsersAsync();
        }

        var model = new RoomCalendarPageViewModel
        {
            Rooms = rooms,
            SelectedRoomEmail = roomEmail,
            SelectedDate = selectedDate,
            Events = events,
            Users = users
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Book(RoomBookingRequestViewModel request)
    {
        if (string.IsNullOrWhiteSpace(request.RoomEmail)
            || string.IsNullOrWhiteSpace(request.OrganizerEmail)
            || request.Start >= request.End)
        {
            TempData["RoomBookingError"] = "Uzupełnij poprawnie dane rezerwacji.";

            return RedirectToAction(nameof(Index), new
            {
                roomEmail = request.RoomEmail,
                date = request.Date.ToString("yyyy-MM-dd")
            });
        }

        var roomEvents = await _graphOrgService.GetRoomCalendarAsync(
            request.RoomEmail,
            request.Date);

        var hasConflict = roomEvents.Any(e =>
            e.Start < request.End &&
            e.End > request.Start);

        if (hasConflict)
        {
            TempData["RoomBookingError"] = "Sala jest już zajęta w wybranym terminie.";

            return RedirectToAction(nameof(Index), new
            {
                roomEmail = request.RoomEmail,
                date = request.Date.ToString("yyyy-MM-dd")
            });
        }

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? "Rezerwacja sali"
            : request.Subject.Trim();

        var result = await _graphOrgService.BookRoomAsync(
            request.OrganizerEmail,
            request.RoomEmail,
            request.Start,
            request.End,
            subject);

        if (result.Success)
        {
            TempData["RoomBookingSuccess"] = result.Message;
        }
        else
        {
            TempData["RoomBookingError"] = result.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            roomEmail = request.RoomEmail,
            date = request.Date.ToString("yyyy-MM-dd")
        });
    }
}
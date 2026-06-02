namespace GraphOrgViewer.Models;

public class RoomCalendarPageViewModel
{
    public List<RoomViewModel> Rooms { get; set; } = [];
    public string? SelectedRoomEmail { get; set; }
    public DateTime SelectedDate { get; set; } = DateTime.Today;
    public List<RoomCalendarEventViewModel> Events { get; set; } = [];
}
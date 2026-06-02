namespace GraphOrgViewer.Models;

public class RoomCalendarEventViewModel
{
    public string Subject { get; set; } = "";
    public string Organizer { get; set; } = "";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string ShowAs { get; set; } = "";
}
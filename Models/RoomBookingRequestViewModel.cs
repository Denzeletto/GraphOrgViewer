namespace GraphOrgViewer.Models;

public class RoomBookingRequestViewModel
{
    public string RoomEmail { get; set; } = "";
    public DateTime Date { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string OrganizerEmail { get; set; } = "";
    public string Subject { get; set; } = "";
}
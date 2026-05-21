namespace GraphOrgViewer.Models;

public class TreantNodeViewModel
{
    public string? InnerHTML { get; set; }
    public string? HtmlClass { get; set; }
    public bool Collapsed { get; set; }
    public List<TreantNodeViewModel> Children { get; set; } = [];
}
namespace Notely.Models;

public class AdminDashboardViewModel
{
    public List<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public List<Note> Notes { get; set; } = new List<Note>();
}

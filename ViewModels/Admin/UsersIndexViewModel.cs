using HalalStocks.Models;

namespace HalalStocks.ViewModels.Admin;

public class UsersIndexViewModel
{
    public IEnumerable<AppUser> Users       { get; set; } = new List<AppUser>();
    public string               Tab         { get; set; } = "admin";
    public int                  AdminCount  { get; set; }
    public int                  PublicCount { get; set; }
}

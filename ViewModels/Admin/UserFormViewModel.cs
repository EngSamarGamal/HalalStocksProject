using HalalStocks.Models;
using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class UserFormViewModel
{
    public string? Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public UserRole Role { get; set; } = UserRole.Editor;
}

using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.ViewModels;

public class RegisterViewModel
{
    public string? Name { get; set; }

    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

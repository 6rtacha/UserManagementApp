namespace UserManagementApp.Models;

public enum UserStatus
{
    Unverified,
    Active,
    Blocked
}

public class User
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserStatus Status { get; set; } = UserStatus.Unverified;

    public string? VerificationToken { get; set; }

    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
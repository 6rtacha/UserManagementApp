using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Controllers;

[Authorize]
[AutoValidateAntiforgeryToken]
public class UsersController : Controller
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users
            .OrderByDescending(u => u.LastLoginAt)
            .ToListAsync();

        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> Block([FromBody] List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        foreach (var user in users)
        {
            user.Status = UserStatus.Blocked;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Selected user(s) blocked successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> Unblock([FromBody] List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id) && u.Status == UserStatus.Blocked)
            .ToListAsync();

        foreach (var user in users)
        {
            user.Status = string.IsNullOrEmpty(user.VerificationToken)
                ? UserStatus.Active
                : UserStatus.Unverified;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Selected user(s) unblocked successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] List<int> userIds)
    {
        if (userIds == null || userIds.Count == 0)
            return BadRequest(new { message = "No users selected" });

        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
        _context.Users.RemoveRange(users);

        await _context.SaveChangesAsync();
        return Ok(new { message = "Selected user(s) deleted." });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUnverified()
    {
        var unverifiedUsers = await _context.Users
            .Where(u => u.Status == UserStatus.Unverified)
            .ToListAsync();

        if (unverifiedUsers.Count == 0)
            return Ok(new { message = "No unverified users found." });

        _context.Users.RemoveRange(unverifiedUsers);
        await _context.SaveChangesAsync();

        return Ok(new { message = $"{unverifiedUsers.Count} unverified user(s) deleted." });
    }
}
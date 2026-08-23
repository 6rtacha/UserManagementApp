using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql; 
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using UserManagementApp.ViewModels;

namespace UserManagementApp.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public AccountController(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var token = Guid.NewGuid().ToString("N");
        var user = new User
        {
            Name = string.IsNullOrWhiteSpace(model.Name) ? "Anonymous" : model.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            Status = UserStatus.Unverified,
            VerificationToken = token,
            LastLoginAt = DateTime.UtcNow
        };

        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var verifyUrl = Url.Action(
                action: nameof(VerifyEmail),
                controller: "Account",
                values: new { token = token, email = user.Email },
                protocol: Request.Scheme
            );

            _ = _emailService.SendVerificationEmailAsync(user.Email, user.Name, verifyUrl!);

            // Automatically sign in the newly registered user and establish a cookie session
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Registration successful! A verification email has been sent to your inbox.";

            return RedirectToAction("Index", "Users");
        }
        catch (DbUpdateException ex)
        {
            // Catch PostgreSQL unique constraint violation (error code 23505) enforced by database index
            if (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
            {
                ModelState.AddModelError("Email", "This email is already registered. (Caught by Database Unique Index)");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "An unexpected database error occurred.");
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        
        if (user.Status == UserStatus.Blocked)
        {
            ModelState.AddModelError(string.Empty, "Your account is blocked. You cannot log in.");
            return View(model);
        }

        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token, string email)
    {
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Invalid verification link.";
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower() && u.VerificationToken == token);
        if (user == null)
        {
            TempData["ErrorMessage"] = "Invalid or expired verification link.";
            return RedirectToAction("Login");
        }

        if (user.Status == UserStatus.Unverified)
        {
            user.Status = UserStatus.Active;
        }

        user.VerificationToken = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your email has been verified! You can now sign in.";
        return RedirectToAction("Login");
    }
}
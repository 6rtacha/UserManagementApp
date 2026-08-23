using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using UserManagementApp.Data;
using UserManagementApp.Models;

namespace UserManagementApp.Middleware;

public class UserStatusMiddleware
{
    private readonly RequestDelegate _next;

    public UserStatusMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out int userId))
            {
                var currentUser = await dbContext.Users.FindAsync(userId);

                if (currentUser == null || currentUser.Status == UserStatus.Blocked)
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                    if (context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                        context.Request.Headers.Accept.ToString().Contains("application/json"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    context.Response.Redirect("/Account/Login?blocked=true");
                    return;
                }
            }
        }

        await _next(context);
    }
}
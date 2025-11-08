using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Barangay.Models;
using Barangay.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Security.Claims;

namespace Barangay.Middleware
{
    public class VerifiedUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<VerifiedUserMiddleware> _logger;

        public VerifiedUserMiddleware(RequestDelegate next, ILogger<VerifiedUserMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            IUserVerificationService userVerificationService,
            SignInManager<ApplicationUser> signInManager,
            IPermissionService permissionService)
        {
            // Skip middleware for non-authenticated users or specific paths
            if (!context.User.Identity.IsAuthenticated ||
                context.Request.Path.StartsWithSegments("/Account") ||
                context.Request.Path.StartsWithSegments("/Admin") ||
                context.Request.Path.StartsWithSegments("/lib") ||
                context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js"))
            {
                await _next(context);
                return;
            }

            var user = await userManager.GetUserAsync(context.User);
            if (user == null)
            {
                await _next(context);
                return;
            }

            // Get user roles
            var roles = await userManager.GetRolesAsync(user);
            
            // Ensure cookie principal role claims are in sync with DB roles
            var roleClaimType = context.User?.Identities.FirstOrDefault()?.RoleClaimType ?? ClaimTypes.Role;
            var principalRoles = context.User?.Claims
                .Where(c => c.Type == roleClaimType)
                .Select(c => c.Value)
                .ToHashSet() ?? new HashSet<string>();
            var dbRoles = roles.ToHashSet();
            if (!principalRoles.SetEquals(dbRoles))
            {
                await signInManager.RefreshSignInAsync(user);
                // Replace the current principal so updated roles apply in this request
                var newPrincipal = await signInManager.CreateUserPrincipalAsync(user);
                context.User = newPrincipal ?? context.User;
                _logger.LogInformation($"Refreshed sign-in for {user.Email} to sync role claims. DB=[{string.Join(",", dbRoles)}] Principal=[{string.Join(",", principalRoles)}]");
            }

            // Always clear the permission session cache for the current user so updates apply immediately
            try
            {
                permissionService.ClearCachedPermissions(user.Id);
            }
            catch
            {
                // Don't block request if cache clear fails
            }
            
            // Allow doctors to access User pages and Doctor pages without verification check
            if (roles.Any(r => r == "Doctor") && 
               (context.Request.Path.StartsWithSegments("/User") || 
                context.Request.Path.StartsWithSegments("/Doctor")))
            {
                // Ensure doctor users are always verified and active
                if (user.Status != "Verified" || !user.IsActive)
                {
                    user.Status = "Verified";
                    user.IsActive = true;
                    await userManager.UpdateAsync(user);
                    _logger.LogInformation($"Doctor {user.Email} status updated to Verified and Active");
                }
                _logger.LogInformation($"Doctor {user.Email} accessing page: {context.Request.Path}");
                await _next(context);
                return;
            }

            // Allow nurses to access User and Nurse pages
            if (roles.Any(r => r == "Nurse") && 
               (context.Request.Path.StartsWithSegments("/User") || 
                context.Request.Path.StartsWithSegments("/Nurse")))
            {
                // Ensure nurse users are always verified and active
                if (user.Status != "Verified" || !user.IsActive)
                {
                    user.Status = "Verified";
                    user.IsActive = true;
                    await userManager.UpdateAsync(user);
                    _logger.LogInformation($"Nurse {user.Email} status updated to Verified and Active");
                }
                _logger.LogInformation($"Nurse {user.Email} accessing page: {context.Request.Path}");
                await _next(context);
                return;
            }

            // Super Admin or Admin users bypass verification check and force activation
            if (roles.Any(r => r == "Admin" || r == "System Administrator" || r == "Admin Staff"))
            {
                // Always force admin users to be verified and active
                user.Status = "Verified";
                user.IsActive = true;
                await userManager.UpdateAsync(user);
                _logger.LogInformation($"Admin user {user.Email} status force updated to Verified and Active");
                
                await _next(context);
                return;
            }

            // Only check verification status for user dashboard access
            if (context.Request.Path.StartsWithSegments("/User"))
            {
                // Allow access if user is verified OR auto-approved (bypass approval page)
                // Accept both "Verified" and "Active" status for auto-approved users
                bool isVerified = (user.Status == "Verified" && user.IsActive) || 
                                 (user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive) ||
                                 (user.Status == "Active" && user.IsApproved && user.VerificationStatus == "Auto Verified" && user.IsActive);
                
                if (isVerified)
                {
                    // Ensure user has the User role if verified (for patients and other users)
                    if (!roles.Contains("User") && !roles.Contains("Admin") && !roles.Contains("Doctor") && !roles.Contains("Nurse"))
                    {
                        await userVerificationService.EnsureUserRoleAssignedAsync(user.Id);

                        // Re-fetch roles and refresh sign-in so the new role applies immediately in this request
                        roles = await userManager.GetRolesAsync(user);

                        var updatedPrincipalRoles = context.User?.Claims
                            .Where(c => c.Type == roleClaimType)
                            .Select(c => c.Value)
                            .ToHashSet() ?? new HashSet<string>();

                        if (roles.Contains("User") && !updatedPrincipalRoles.Contains("User"))
                        {
                            await signInManager.RefreshSignInAsync(user);
                            var refreshedPrincipal = await signInManager.CreateUserPrincipalAsync(user);
                            context.User = refreshedPrincipal ?? context.User;
                            _logger.LogInformation($"Refreshed sign-in for {user.Email} after assigning 'User' role.");
                        }
                    }
                    await _next(context);
                    return;
                }

                // Handle unverified users trying to access user pages
                _logger.LogWarning($"Unverified user {user.Email} attempted to access {context.Request.Path}");
                if (IsAjaxOrApiRequest(context.Request))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"success\":false,\"message\":\"Your account is not verified yet. Please wait for approval.\"}");
                }
                else
                {
                    context.Response.Redirect("/Account/WaitingForApproval");
                }
                return;
            }

            await _next(context);
        }

        private static bool IsAjaxOrApiRequest(HttpRequest request)
        {
            var accept = request.Headers["Accept"].ToString();
            var requestedWith = request.Headers["X-Requested-With"].ToString();
            var secFetchMode = request.Headers["Sec-Fetch-Mode"].ToString();

            return (!string.IsNullOrEmpty(requestedWith) && requestedWith == "XMLHttpRequest")
                   || (!string.IsNullOrEmpty(accept) && (accept.Contains("application/json") || accept.Contains("text/json")))
                   || (!string.IsNullOrEmpty(secFetchMode) && (secFetchMode == "cors" || secFetchMode == "same-origin"));
        }
    }

    public static class VerifiedUserMiddlewareExtensions
    {
        public static IApplicationBuilder UseVerifiedUserMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<VerifiedUserMiddleware>();
        }
    }
}
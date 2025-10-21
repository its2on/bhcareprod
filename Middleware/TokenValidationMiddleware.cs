using Barangay.Services;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Barangay.Middleware
{
    public class TokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TokenValidationMiddleware> _logger;

        public TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ITokenService tokenService)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "";
            
            // Skip token validation for public pages
            if (IsPublicPath(path))
            {
                await _next(context);
                return;
            }

            // Skip token validation for authenticated users accessing protected paths
            // This allows normal authenticated users to access their role-based pages without tokens
            if (context.User.Identity?.IsAuthenticated == true)
            {
                _logger.LogInformation("Authenticated user {UserName} accessing path {Path}, skipping token validation", 
                    context.User.Identity.Name, path);
                await _next(context);
                return;
            }

            // Only check for tokens if user is NOT authenticated and accessing a token-protected route
            if (IsTokenProtectedPath(path))
            {
                var token = ExtractTokenFromRequest(context);
                
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("Access denied to {Path}: No token provided and user not authenticated", path);
                    await HandleUnauthorizedAccess(context, "Token required");
                    return;
                }

                var urlToken = await tokenService.ValidateTokenAsync(token);
                if (urlToken == null)
                {
                    _logger.LogWarning("Access denied to {Path}: Invalid or expired token", path);
                    await HandleUnauthorizedAccess(context, "Invalid or expired token");
                    return;
                }

                // Check if user has permission to access this resource type
                if (!HasPermissionForResourceType(context, urlToken.ResourceType))
                {
                    _logger.LogWarning("Access denied to {Path}: User {UserId} lacks permission for {ResourceType}", 
                        path, urlToken.ResourceId, urlToken.ResourceType);
                    await HandleUnauthorizedAccess(context, "Insufficient permissions");
                    return;
                }

                // Mark token as used
                var ipAddress = context.Connection.RemoteIpAddress?.ToString();
                var userAgent = context.Request.Headers["User-Agent"].ToString();
                await tokenService.MarkTokenAsUsedAsync(token, ipAddress, userAgent);

                // Add token information to context for use in controllers/pages
                context.Items["TokenInfo"] = urlToken;
                context.Items["ResourceId"] = urlToken.ResourceId;
                context.Items["ResourceType"] = urlToken.ResourceType;

                _logger.LogInformation("Token access granted to {Path} for {ResourceType} ID {ResourceId}", 
                    path, urlToken.ResourceType, urlToken.ResourceId);
            }

            await _next(context);
        }

        private bool IsPublicPath(string path)
        {
            var publicPaths = new[]
            {
                "/",
                "/index",
                "/privacy",
                "/terms",
                "/dataprivacy",
                "/about",
                "/contact",
                "/account/login",
                "/account/logout",
                "/account/register",
                "/account/accessdenied",
                "/error",
                "/favicon.ico",
                "/css/",
                "/js/",
                "/images/",
                "/lib/",
                "/_framework/"
            };

            return publicPaths.Any(publicPath => path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool IsTokenProtectedPath(string path)
        {
            var protectedPaths = new[]
            {
                "/user/",
                "/nurse/",
                "/doctor/",
                "/admin/"
            };

            return protectedPaths.Any(protectedPath => path.StartsWith(protectedPath, StringComparison.OrdinalIgnoreCase));
        }

        private string? ExtractTokenFromRequest(HttpContext context)
        {
            // Try to get token from query string first
            if (context.Request.Query.TryGetValue("token", out var tokenValue))
            {
                return tokenValue.ToString();
            }

            // Try to get token from headers
            if (context.Request.Headers.TryGetValue("X-Access-Token", out var headerValue))
            {
                return headerValue.ToString();
            }

            // Try to get token from cookies
            if (context.Request.Cookies.TryGetValue("access_token", out var cookieValue))
            {
                return cookieValue;
            }

            return null;
        }

        private bool HasPermissionForResourceType(HttpContext context, string resourceType)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                return false;
            }

            // Check if user has the appropriate role for the resource type
            return resourceType.ToLowerInvariant() switch
            {
                "user" => context.User.IsInRole("User") || context.User.IsInRole("Admin") || context.User.IsInRole("System Administrator"),
                "nurse" => context.User.IsInRole("Nurse") || context.User.IsInRole("Head Nurse") || context.User.IsInRole("Admin") || context.User.IsInRole("System Administrator"),
                "doctor" => context.User.IsInRole("Doctor") || context.User.IsInRole("Admin") || context.User.IsInRole("System Administrator"),
                "admin" => context.User.IsInRole("Admin") || context.User.IsInRole("System Administrator"),
                _ => false
            };
        }

        private async Task HandleUnauthorizedAccess(HttpContext context, string message)
        {
            // Check if this is an AJAX/API request
            var accept = context.Request.Headers["Accept"].ToString();
            var requestedWith = context.Request.Headers["X-Requested-With"].ToString();
            var secFetchMode = context.Request.Headers["Sec-Fetch-Mode"].ToString();

            var isAjaxRequest = (!string.IsNullOrEmpty(requestedWith) && requestedWith == "XMLHttpRequest")
                               || (!string.IsNullOrEmpty(accept) && (accept.Contains("application/json") || accept.Contains("text/json")))
                               || (!string.IsNullOrEmpty(secFetchMode) && (secFetchMode == "cors" || secFetchMode == "same-origin"));

            if (isAjaxRequest)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                
                var response = new
                {
                    error = "Unauthorized",
                    message = message,
                    timestamp = DateTime.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            else
            {
                // Redirect to access denied page
                context.Response.Redirect("/Account/AccessDenied");
            }
        }
    }
}

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RechargePlatform.Common.Constants;
using RechargePlatform.Common.DTOs;

namespace RechargePlatform.Common.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyMiddleware> _logger;
    private readonly string _configuredApiKey;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _configuredApiKey = configuration[AuthConstants.ConfigApiKeyPath] 
            ?? Environment.GetEnvironmentVariable("RECHARGE_API_KEY") 
            ?? AuthConstants.DefaultApiKey;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Skip auth for Swagger, health checks, and CORS preflight OPTIONS requests
        if (context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName, out var extractedApiKeyValues))
        {
            _logger.LogWarning("Authentication failed: Missing {HeaderName} header for path {Path}", AuthConstants.ApiKeyHeaderName, path);
            await ReturnUnauthorizedResponse(context, "API Key is required in 'X-Api-Key' header.");
            return;
        }

        var extractedApiKey = extractedApiKeyValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(extractedApiKey) || !string.Equals(extractedApiKey, _configuredApiKey, StringComparison.Ordinal))
        {
            // NEVER log the extracted key value
            _logger.LogWarning("Authentication failed: Invalid API Key provided for path {Path}", path);
            await ReturnUnauthorizedResponse(context, "Invalid API Key provided.");
            return;
        }

        await _next(context);
    }

    private static async Task ReturnUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            ErrorCode = "AUTH_UNAUTHORIZED",
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}

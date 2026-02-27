using Tanaste.Api.Services;
using Tanaste.Domain.Contracts;

namespace Tanaste.Api.Middleware;

/// <summary>
/// Validates the <c>X-Api-Key</c> request header against hashed keys stored in the database.
///
/// Behaviour:
/// • Header absent  → request passes through (enables Swagger UI and local browser access).
/// • Header present + valid hash found → request passes through.
/// • Header present + hash not found  → 401 Unauthorized.
///
/// External integrations (Radarr, Sonarr, scripts) MUST include the header.
/// The <c>GET /system/status</c> endpoint works without a key (useful for URL-only checks)
/// but also validates correctly when a key is provided.
///
/// SECURITY: The raw key value from the header is NEVER logged.
/// </summary>
public sealed class ApiKeyMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Api-Key";

    public async Task InvokeAsync(HttpContext ctx, IApiKeyRepository repo)
    {
        if (!ctx.Request.Headers.TryGetValue(HeaderName, out var rawValues))
        {
            // No header — unauthenticated pass-through.
            await next(ctx).ConfigureAwait(false);
            return;
        }

        var rawKey = rawValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "X-Api-Key header is empty." })
                               .ConfigureAwait(false);
            return;
        }

        // Hash the incoming key and look it up — the plaintext is NEVER logged.
        var hashedKey = ApiKeyService.HashKey(rawKey);
        var match     = await repo.FindByHashedKeyAsync(hashedKey, ctx.RequestAborted)
                                   .ConfigureAwait(false);

        if (match is null)
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await ctx.Response.WriteAsJsonAsync(new { error = "Invalid API key." })
                               .ConfigureAwait(false);
            return;
        }

        // Key is valid — annotate the context for downstream use if needed.
        ctx.Items["ApiKeyId"]    = match.Id.ToString();
        ctx.Items["ApiKeyLabel"] = match.Label;

        await next(ctx).ConfigureAwait(false);
    }
}

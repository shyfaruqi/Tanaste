using Microsoft.Extensions.Options;
using Tanaste.Api.Models;
using Tanaste.Ingestion.Contracts;
using Tanaste.Ingestion.Models;

namespace Tanaste.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/ingestion")
                       .WithTags("Ingestion");

        group.MapPost("/scan", async (
            ScanRequest? request,
            IIngestionEngine engine,
            IOptions<IngestionOptions> opts,
            CancellationToken ct) =>
        {
            var rootPath = request?.RootPath
                ?? opts.Value.WatchDirectory;

            if (string.IsNullOrWhiteSpace(rootPath))
                return Results.BadRequest(
                    "No root_path provided and Ingestion:WatchDirectory is not configured.");

            if (!Directory.Exists(rootPath))
                return Results.BadRequest($"Directory does not exist: {rootPath}");

            var operations = await engine.DryRunAsync(rootPath, ct);
            var response = new ScanResponse
            {
                Operations = operations
                    .Select(PendingOperationDto.FromDomain)
                    .ToList(),
            };

            return Results.Ok(response);
        })
        .WithName("TriggerScan")
        .WithSummary("Simulate a library scan and return pending operations without mutating files.")
        .Produces<ScanResponse>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest);

        return app;
    }
}

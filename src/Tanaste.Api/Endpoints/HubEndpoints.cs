using Tanaste.Api.Models;
using Tanaste.Storage.Contracts;

namespace Tanaste.Api.Endpoints;

public static class HubEndpoints
{
    public static IEndpointRouteBuilder MapHubEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/hubs")
                       .WithTags("Hubs");

        group.MapGet("/", async (
            IHubRepository hubRepo,
            CancellationToken ct) =>
        {
            var hubs = await hubRepo.GetAllAsync(ct);
            var dtos = hubs.Select(HubDto.FromDomain).ToList();
            return Results.Ok(dtos);
        })
        .WithName("GetAllHubs")
        .WithSummary("List all media hubs with their works and canonical metadata values.")
        .Produces<List<HubDto>>(StatusCodes.Status200OK);

        return app;
    }
}

using HomeMesh.Application.Members;
using HomeMesh.Application.Sync;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.WebApi.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        static async Task<IResult> UpdateMember(
            string networkId,
            string memberId,
            UpdateMemberRequest request,
            MemberService memberService,
            CancellationToken cancellationToken)
        {
            try
            {
                var member = await memberService.UpdateAsync(networkId, memberId, request, cancellationToken);
                return Results.Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }

        app.MapPost("/api/join/{networkId}", async Task<IResult> (
            string networkId,
            JoinNetworkRequest request,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var member = await memberService.JoinAsync(networkId, request, cancellationToken);
                return Results.Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapGet("/api/networks/{networkId}/members", async Task<IResult> (
            string networkId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await memberService.ListAsync(networkId, cancellationToken));
        });

        app.MapPost("/api/networks/{networkId}/members/sync", async Task<IResult> (
            string networkId,
            NetworkSyncService syncService,
            CancellationToken cancellationToken) =>
        {
            var result = await syncService.SyncMembersAsync(networkId, cancellationToken);
            return Results.Ok(result);
        });

        app.MapGet("/api/networks/{networkId}/members/sync-state", async Task<IResult> (
            string networkId,
            HomeMeshDbContext db,
            CancellationToken cancellationToken) =>
        {
            var states = await db.ProviderSyncStates
                .Where(x => x.ResourceId == networkId && x.ResourceType == "Members")
                .OrderBy(x => x.Provider)
                .ToListAsync(cancellationToken);

            return Results.Ok(states);
        });

        app.MapGet("/api/networks/{networkId}/members/{memberId}", async Task<IResult> (
            string networkId,
            string memberId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            var member = await memberService.GetAsync(networkId, memberId, cancellationToken);
            if (member is null)
            {
                return Results.NotFound(new { error = "Member not found." });
            }

            return Results.Ok(member);
        });

        app.MapPost("/api/networks/{networkId}/members/{memberId}", UpdateMember);

        app.MapPost("/api/networks/{networkId}/members/{memberId}/authorize", async Task<IResult> (
            string networkId,
            string memberId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var member = await memberService.UpdateAsync(
                    networkId,
                    memberId,
                    new UpdateMemberRequest(Authorized: true),
                    cancellationToken);

                return Results.Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapPost("/api/networks/{networkId}/members/{memberId}/deauthorize", async Task<IResult> (
            string networkId,
            string memberId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var member = await memberService.UpdateAsync(
                    networkId,
                    memberId,
                    new UpdateMemberRequest(Authorized: false),
                    cancellationToken);

                return Results.Ok(member);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        app.MapDelete("/api/networks/{networkId}/members/{memberId}", async Task<IResult> (
            string networkId,
            string memberId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            var deleted = await memberService.RemoveAsync(networkId, memberId, cancellationToken);
            if (!deleted)
            {
                return Results.NotFound(new { error = "Member not found." });
            }

            return Results.NoContent();
        });

        return app;
    }
}

using HomeMesh.Application.Members;

namespace HomeMesh.WebApi.Endpoints;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/networks/{networkId}/members", async (
            string networkId,
            MemberService memberService,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await memberService.ListAsync(networkId, cancellationToken));
        });

        app.MapGet("/api/networks/{networkId}/members/{memberId}", async (
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

        app.MapPatch("/api/networks/{networkId}/members/{memberId}", async (
            string networkId,
            string memberId,
            UpdateMemberRequest request,
            MemberService memberService,
            CancellationToken cancellationToken) =>
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
        });

        app.MapPost("/api/networks/{networkId}/members/{memberId}/authorize", async (
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

        app.MapPost("/api/networks/{networkId}/members/{memberId}/deauthorize", async (
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

        app.MapDelete("/api/networks/{networkId}/members/{memberId}", async (
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

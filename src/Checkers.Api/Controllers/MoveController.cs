using Checkers.Api.Contracts;
using Checkers.Api.Logging;
using Checkers.Application.Moves;
using Checkers.Application.Strength;
using Microsoft.AspNetCore.Mvc;

namespace Checkers.Api.Controllers;

[ApiController]
[Route("v1/move")]
public sealed class MoveController(
    SuggestMoveService suggestions,
    ValidateMoveService validation,
    LegalMovesService legalMoves,
    StrengthPolicy policy,
    TimeProvider timeProvider,
    ILogger<MoveController> logger) : ControllerBase
{
    [HttpPost("suggest")]
    public async Task<ActionResult<SuggestMoveResponse>> Suggest(SuggestMoveRequest request)
    {
        var started = timeProvider.GetTimestamp();
        using var hardLimit = new CancellationTokenSource(policy.ResolveHardTimeout(request.Limits), timeProvider);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(hardLimit.Token, HttpContext.RequestAborted);
        MoveSuggestion? suggestion = null;
        try
        {
            suggestion = await suggestions.SuggestAsync(request.State.Position, request.Level, request.Limits, deadline.Token);
            return SuggestMoveResponse.From(suggestion, timeProvider.GetElapsedTime(started));
        }
        catch (OperationCanceledException) when (hardLimit.IsCancellationRequested)
        {
            return Problem(statusCode: StatusCodes.Status504GatewayTimeout, title: "Hard time limit exceeded");
        }
        finally
        {
            var timeMs = (long)timeProvider.GetElapsedTime(started).TotalMilliseconds;
            logger.SuggestRequestCompleted(
                HttpContext.TraceIdentifier,
                timeMs,
                suggestion?.Depth,
                suggestion?.Nodes,
                suggestion?.TablebaseHit);
        }
    }

    [HttpPost("validate")]
    public ValidateMoveResponse Validate(ValidateMoveRequest request) =>
        new(validation.IsLegal(request.Position, request.Move));

    [HttpPost("legal")]
    public LegalMovesResponse Legal(LegalMovesRequest request) =>
        LegalMovesResponse.From(legalMoves.List(request.Position));
}

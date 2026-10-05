using Checkers.Application.Moves;
using Checkers.Domain;
using Microsoft.AspNetCore.Diagnostics;

namespace Checkers.Api.ErrorHandling;

/// <summary>Answers 422 for a well-formed request whose position or move cannot be processed.</summary>
public sealed class UnprocessableRequestExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not (InvalidPdnException or NoLegalMovesException))
        {
            return ValueTask.FromResult(false);
        }

        httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = StatusCodes.Status422UnprocessableEntity,
                Title = "Unprocessable PDN",
                Detail = exception.Message,
            },
        });
    }
}

using System.ComponentModel.DataAnnotations;
using Checkers.Application.Strength;

namespace Checkers.Api.Contracts;

public sealed record SuggestMoveRequest(
    [Required, AllowedValues("checkers-8x8")] string GameId,
    [Required] PositionState State,
    StrengthLevel? Level,
    RequestedLimits? Limits);

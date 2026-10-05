using System.ComponentModel.DataAnnotations;

namespace Checkers.Api.Contracts;

public sealed record PositionState(
    [Required, AllowedValues("PDN")] string Notation,
    [Required] string Position);

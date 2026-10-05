using System.ComponentModel.DataAnnotations;

namespace Checkers.Api.Contracts;

public sealed record ValidateMoveRequest([Required] string Position, [Required] string Move);

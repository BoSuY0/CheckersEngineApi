using System.ComponentModel.DataAnnotations;

namespace Checkers.Api.Contracts;

public sealed record LegalMovesRequest([Required] string Position);

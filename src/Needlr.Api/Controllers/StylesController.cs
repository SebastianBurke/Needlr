using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Needlr.Application.Abstractions.Persistence;
using Needlr.Contracts.Portfolio;

namespace Needlr.Api.Controllers;

/// <summary>
/// Read-only endpoints for the global tattoo-style catalog. The canonical list backs the
/// discovery filter chips and the artist/portfolio style pickers; the data is deliberately
/// public (no PII, no per-user data).
/// </summary>
[ApiController]
[Route("api/styles")]
[AllowAnonymous]
public sealed class StylesController(ITattooStyleRepository styles) : ControllerBase
{
    private readonly ITattooStyleRepository _styles = styles;

    /// <summary>Returns the seeded canonical styles. Promoted freeform tags are excluded.</summary>
    [HttpGet("canonical")]
    public async Task<IActionResult> ListCanonical(CancellationToken cancellationToken)
    {
        var rows = await _styles.ListCanonicalAsync(cancellationToken);
        var response = rows
            .Select(s => new TattooStyleResponse(s.Id, s.Name, s.Slug, s.IsCanonical))
            .ToList();
        return Ok(response);
    }
}

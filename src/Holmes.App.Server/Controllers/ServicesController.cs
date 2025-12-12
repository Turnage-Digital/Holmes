using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public sealed class ServicesController(IMediator mediator) : ControllerBase
{
    /// <summary>
    ///     Returns all available service types.
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceTypeDto>>> GetServiceTypes(
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new ListServiceTypesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
    }
}
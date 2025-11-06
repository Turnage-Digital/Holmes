using Holmes.App.Server.Security;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Route("customers")]
public class CustomersController : ControllerBase
{
    private readonly CustomersDbContext _customersDbContext;
    private readonly IMediator _mediator;
    private readonly IUserContext _userContext;
    private readonly UsersDbContext _usersDbContext;

    public CustomersController(
        IMediator mediator,
        CustomersDbContext customersDbContext,
        UsersDbContext usersDbContext,
        IUserContext userContext
    )
    {
        _mediator = mediator;
        _customersDbContext = customersDbContext;
        _usersDbContext = usersDbContext;
        _userContext = userContext;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IReadOnlyCollection<CustomerSummaryResponse>>> GetCustomers(
        CancellationToken cancellationToken
    )
    {
        var caller = await EnsureUserAsync(cancellationToken);

        var query = _customersDbContext.CustomerDirectory.AsNoTracking();

        if (!await IsGlobalAdminAsync(caller, cancellationToken))
        {
            var customerIds = await _customersDbContext.CustomerAdmins
                .AsNoTracking()
                .Where(a => a.UserId == caller.ToString())
                .Select(a => a.CustomerId)
                .ToListAsync(cancellationToken);

            query = query.Where(c => customerIds.Contains(c.CustomerId));
        }

        var results = await query
            .OrderBy(c => c.Name)
            .Select(c => new CustomerSummaryResponse(
                c.CustomerId,
                c.Name,
                c.Status,
                c.CreatedAt,
                c.AdminCount))
            .ToListAsync(cancellationToken);

        return Ok(results);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CustomerSummaryResponse>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken
    )
    {
        var caller = await EnsureUserAsync(cancellationToken);

        if (!await IsGlobalAdminAsync(caller, cancellationToken))
        {
            return Forbid();
        }

        var id = await _mediator.Send(new RegisterCustomerCommand(
            request.Name,
            DateTimeOffset.UtcNow), cancellationToken);

        // promote caller to admin
        await _mediator.Send(new AssignCustomerAdminCommand(
            id,
            caller,
            caller,
            DateTimeOffset.UtcNow), cancellationToken);

        var directory = await _customersDbContext.CustomerDirectory.AsNoTracking()
            .SingleAsync(c => c.CustomerId == id.ToString(), cancellationToken);

        return CreatedAtAction(nameof(GetCustomerById), new { customerId = id }, new CustomerSummaryResponse(
            directory.CustomerId,
            directory.Name,
            directory.Status,
            directory.CreatedAt,
            directory.AdminCount));
    }

    [HttpGet("{customerId}")]
    [Authorize]
    public async Task<ActionResult<CustomerDetailResponse>> GetCustomerById(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsed, caller, cancellationToken))
        {
            return Forbid();
        }

        var directory = await _customersDbContext.CustomerDirectory.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (directory is null)
        {
            return NotFound();
        }

        var admins = await _customersDbContext.CustomerAdmins.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => new CustomerAdminResponse(a.UserId, a.AssignedBy.ToString(), a.AssignedAt))
            .ToListAsync(cancellationToken);

        return Ok(new CustomerDetailResponse(
            directory.CustomerId,
            directory.Name,
            directory.Status,
            directory.CreatedAt,
            admins));
    }

    [HttpPost("{customerId}/admins")]
    [Authorize]
    public async Task<IActionResult> AssignCustomerAdmin(
        string customerId,
        [FromBody] ModifyCustomerAdminRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsedCustomer) || !Ulid.TryParse(request.UserId, out var parsedUser))
        {
            return BadRequest("Invalid id format.");
        }

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsedCustomer, caller, cancellationToken))
        {
            return Forbid();
        }

        var result = await _mediator.Send(new AssignCustomerAdminCommand(
            UlidId.FromUlid(parsedCustomer),
            UlidId.FromUlid(parsedUser),
            caller,
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{customerId}/admins")]
    [Authorize]
    public async Task<IActionResult> RemoveCustomerAdmin(
        string customerId,
        [FromBody] ModifyCustomerAdminRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsedCustomer) || !Ulid.TryParse(request.UserId, out var parsedUser))
        {
            return BadRequest("Invalid id format.");
        }

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsedCustomer, caller, cancellationToken))
        {
            return Forbid();
        }

        var result = await _mediator.Send(new RemoveCustomerAdminCommand(
            UlidId.FromUlid(parsedCustomer),
            UlidId.FromUlid(parsedUser),
            caller,
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    private async Task<bool> HasCustomerAccessAsync(Ulid customerId, UlidId caller, CancellationToken cancellationToken)
    {
        if (await IsGlobalAdminAsync(caller, cancellationToken))
        {
            return true;
        }

        return await _customersDbContext.CustomerAdmins
            .AsNoTracking()
            .AnyAsync(a => a.CustomerId == customerId.ToString() && a.UserId == caller.ToString(), cancellationToken);
    }

    private async Task<bool> IsGlobalAdminAsync(UlidId caller, CancellationToken cancellationToken)
    {
        return await _usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(r =>
                    r.UserId == caller.ToString() &&
                    r.Role == UserRole.Admin &&
                    r.CustomerId == null,
                cancellationToken);
    }

    private async Task<UlidId> EnsureUserAsync(CancellationToken cancellationToken)
    {
        return await _mediator.Send(new RegisterExternalUserCommand(
            _userContext.Issuer,
            _userContext.Subject,
            _userContext.Email,
            _userContext.DisplayName,
            _userContext.AuthenticationMethod,
            DateTimeOffset.UtcNow), cancellationToken);
    }

    public sealed record CreateCustomerRequest(string Name);

    public sealed record ModifyCustomerAdminRequest(string UserId);

    public sealed record CustomerSummaryResponse(
        string CustomerId,
        string Name,
        CustomerStatus Status,
        DateTimeOffset CreatedAt,
        int AdminCount
    );

    public sealed record CustomerAdminResponse(
        string UserId,
        string AssignedBy,
        DateTimeOffset AssignedAt
    );

    public sealed record CustomerDetailResponse(
        string CustomerId,
        string Name,
        CustomerStatus Status,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<CustomerAdminResponse> Admins
    );
}
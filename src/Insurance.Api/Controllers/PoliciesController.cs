using Insurance.Api.Contracts.Policies;
using Insurance.Api.Domain.Enums;
using Insurance.Api.Domain.Exceptions;
using Insurance.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Insurance.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PoliciesController : ControllerBase
{
    private readonly IPolicyService _policyService;

    public PoliciesController(IPolicyService policyService)
    {
        _policyService = policyService;
    }

    [HttpPost]
    public async Task<ActionResult<PolicyResponse>> Create(
        [FromBody] CreatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _policyService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PolicyResponse>>> GetAll(
        [FromQuery] int? customerId,
        [FromQuery] PolicyType? type,
        [FromQuery] bool? active,
        [FromQuery] PolicyStatus? status,
        CancellationToken cancellationToken)
    {
        var policies = await _policyService.GetAllAsync(customerId, type, active, status, cancellationToken);
        return Ok(policies);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PolicyResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var policy = await _policyService.GetByIdAsync(id, cancellationToken);
            return Ok(policy);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PolicyResponse>> Update(
        int id,
        [FromBody] UpdatePolicyRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _policyService.UpdateAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<PolicyResponse>> Cancel(int id, CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await _policyService.CancelAsync(id, cancellationToken);
            return Ok(cancelled);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

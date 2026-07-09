using Asp.Versioning;
using Bookings.Api.Common;
using Bookings.Application.Common.Pagination;
using Bookings.Application.Resources;
using Bookings.Application.Resources.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Controllers;

/// <summary>
/// CRUD endpoints for bookable resources. Controllers stay thin: they translate
/// between HTTP and the <see cref="IResourceService"/> use cases and map results
/// to the appropriate status codes.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ResourcesController : ControllerBase
{
    private readonly IResourceService _resourceService;

    public ResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    /// <summary>Lists resources, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ResourceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ResourceResponse>>> GetAll(
        [FromQuery] PaginationParameters pagination,
        CancellationToken cancellationToken)
    {
        var resources = await _resourceService.GetAllAsync(pagination.Page, pagination.PageSize, cancellationToken);
        return Ok(resources);
    }

    /// <summary>Gets a single resource by id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var resource = await _resourceService.GetByIdAsync(id, cancellationToken);
        return resource is null ? NotFound() : Ok(resource);
    }

    /// <summary>Creates a new resource.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ResourceResponse>> Create(
        CreateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _resourceService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Replaces a resource's mutable fields.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResourceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResourceResponse>> Update(
        Guid id,
        UpdateResourceRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _resourceService.UpdateAsync(id, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a resource.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _resourceService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}

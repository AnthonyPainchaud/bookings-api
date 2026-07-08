using Bookings.Application.Resources.Dtos;

namespace Bookings.Application.Resources;

/// <summary>
/// Use-case operations for managing resources. The controller depends on this
/// abstraction rather than a concrete implementation.
/// </summary>
public interface IResourceService
{
    Task<IReadOnlyList<ResourceResponse>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the resource, or <c>null</c> if no resource has the given id.</summary>
    Task<ResourceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ResourceResponse> CreateAsync(CreateResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns the updated resource, or <c>null</c> if it does not exist.</summary>
    Task<ResourceResponse?> UpdateAsync(Guid id, UpdateResourceRequest request, CancellationToken cancellationToken = default);

    /// <summary>Returns <c>true</c> if a resource was deleted, <c>false</c> if none existed.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

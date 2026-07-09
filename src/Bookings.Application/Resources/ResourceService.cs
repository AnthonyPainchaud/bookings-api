using Bookings.Application.Common.Interfaces;
using Bookings.Application.Common.Pagination;
using Bookings.Application.Resources.Dtos;
using Bookings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Application.Resources;

/// <summary>
/// Default implementation of <see cref="IResourceService"/>, backed by the
/// application's EF Core context.
/// </summary>
public class ResourceService : IResourceService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public ResourceService(IApplicationDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<ResourceResponse>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: read-only query, so skip change-tracking overhead.
        var query = _dbContext.Resources.AsNoTracking().OrderBy(r => r.Name);

        var totalCount = await query.CountAsync(cancellationToken);

        // Materialize first, then map in memory — ToResponse() is a plain method
        // and cannot be translated to SQL inside the query.
        var resources = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ResourceResponse>(
            resources.Select(r => r.ToResponse()).ToList(), page, pageSize, totalCount);
    }

    public async Task<ResourceResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _dbContext.Resources
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return resource?.ToResponse();
    }

    public async Task<ResourceResponse> CreateAsync(CreateResourceRequest request, CancellationToken cancellationToken = default)
    {
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            Capacity = request.Capacity,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow()
        };

        _dbContext.Resources.Add(resource);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return resource.ToResponse();
    }

    public async Task<ResourceResponse?> UpdateAsync(Guid id, UpdateResourceRequest request, CancellationToken cancellationToken = default)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (resource is null)
        {
            return null;
        }

        resource.Name = request.Name;
        resource.Description = request.Description;
        resource.Type = request.Type;
        resource.Capacity = request.Capacity;
        resource.IsActive = request.IsActive;
        resource.UpdatedAt = _timeProvider.GetUtcNow();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return resource.ToResponse();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _dbContext.Resources
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (resource is null)
        {
            return false;
        }

        _dbContext.Resources.Remove(resource);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}

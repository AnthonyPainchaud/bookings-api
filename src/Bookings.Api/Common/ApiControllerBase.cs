using Bookings.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace Bookings.Api.Common;

/// <summary>
/// Base controller that centralizes the mapping from an application
/// <see cref="Error"/> to an HTTP <see cref="ProblemDetails"/> response, so each
/// controller action stays a simple success/failure branch.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected ActionResult HandleError(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Problem(detail: error.Message, statusCode: statusCode);
    }
}

using System.Security.Claims;
using ConnectGrowAPI.Models;
using ConnectGrowAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ConnectGrowAPI.Controllers;

//api custom base controller that holds common fucntionality for all the other controllers to inherit from
//avoids code duplication and manages code functionality, 


[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected Guid CurrentUserId
    {
        get
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
 
    protected bool IsAdmin => User.IsInRole(RoleNames.Admin);
 
    protected ActionResult Problem(Result result) => result.ErrorType switch
    {
        ErrorType.NotFound   => NotFound(new ApiError(result.Error!)),
        ErrorType.Validation => BadRequest(new ApiError(result.Error!)),
        ErrorType.Conflict   => Conflict(new ApiError(result.Error!)),
        ErrorType.Forbidden  => StatusCode(StatusCodes.Status403Forbidden, new ApiError(result.Error!)),
        _                    => StatusCode(StatusCodes.Status500InternalServerError,
                                    new ApiError(result.Error ?? "An unexpected error occurred."))
    };
}
 

public record ApiError(string Message);
 
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Ecommerce
{
    public abstract class EcommerceController : ControllerBase
    {
        protected string UserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("No user id in token");

        protected ActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.Success && result.Data != null) return Ok(result.Data);

            if (result.Success) return NoContent();

            return result.ErrorType switch
            {
                ErrorType.NotFound => NotFound(new { error = result.Error }),
                ErrorType.Unauthorized => Unauthorized(new { error = result.Error }),
                ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { error = result.Error }),
                ErrorType.Conflict => Conflict(new { error = result.Error }),
                ErrorType.BadRequest => BadRequest(new { error = result.Error }),
                _ => BadRequest(new { error = result.Error })
            };
        }
    }
}

using Microsoft.AspNetCore.Mvc;

namespace Ecommerce
{
    public abstract class ApiControllerBase : ControllerBase
    {
        protected IActionResult ToActionResult<T>(ServiceResult<T> result)
        {
            if (result.Success) return Ok(result.Data);

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

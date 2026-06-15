using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Domain.Common;

namespace VirtualWardrobe.Api.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected ObjectResult ProblemFromError(ResultError error, string title)
    {
        var statusCode = error.Code switch
        {
            "forbidden" => StatusCodes.Status403Forbidden,
            "not_found" => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status400BadRequest
        };
        return Problem(title: title, detail: error.Message, statusCode: statusCode)!;
    }

    protected ActionResult<TResponse> ToActionResult<TDomain, TResponse>(
        Result<TDomain> result,
        Func<TDomain, TResponse> map,
        int successStatusCode,
        string problemTitle)
    {
        if (result.IsFailure)
            return ProblemFromError(result.Error, problemTitle);

        var response = map(result.Value);
        return successStatusCode == StatusCodes.Status201Created
            ? CreatedAtAction(null, response)
            : Ok(response);
    }

    protected static bool TryParseCategory(string category, out ClothingCategory parsedCategory)
    {
        return Enum.TryParse(category, true, out parsedCategory)
               && Enum.IsDefined(parsedCategory);
    }
}

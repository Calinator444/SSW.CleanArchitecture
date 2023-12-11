using Microsoft.AspNetCore.Diagnostics;
using SSW.CleanArchitecture.Application.Common.Exceptions;

namespace SSW.CleanArchitecture.WebApi.Filters;

public class GlobalExceptionHandler : IExceptionHandler
{
    private static readonly IDictionary<Type, Func<HttpContext, Exception, IResult>> ExceptionHandlers =
        new Dictionary<Type, Func<HttpContext, Exception, IResult>>
        {
            { typeof(ValidationException), HandleValidationException },
            { typeof(NotFoundException), HandleNotFoundException }
        };

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var result = HandleException(httpContext, exception);
        await result.ExecuteAsync(httpContext);
        return true;
    }

    private static IResult HandleException(HttpContext context, Exception exception)
    {
        if (ExceptionHandlers.TryGetValue(exception.GetType(), out var handler))
            return handler.Invoke(context, exception);

        return HandleGenericException(context, exception);
    }

    private static IResult HandleValidationException(HttpContext context, Exception exception)
    {
        var validationException = exception as ValidationException ??
                                  throw new InvalidOperationException("Exception is not of type ValidationException");

        return Results.ValidationProblem(validationException.Errors,
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.1");
    }

    private static IResult HandleNotFoundException(HttpContext context, Exception exception) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound,
            title: "The specified resource was not found.",
            type: "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            detail: exception.Message);

    private static IResult HandleGenericException(HttpContext context, Exception exception) =>
        Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
}
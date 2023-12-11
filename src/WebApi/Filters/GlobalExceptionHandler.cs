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

    // public static void UseExceptionFilter(this WebApplication app)
    // {
    //     app.UseExceptionHandler(exceptionHandlerApp
    //         => exceptionHandlerApp.Run(async context =>
    //         {
    //             var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error!;
    //
    //             await context
    //                 .HandleException(exception)
    //                 .ExecuteAsync(context);
    //         }));
    // }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken cancellationToken)
    {
        var result = HandleException(httpContext, exception);
        await result.ExecuteAsync(httpContext);
        return true;
    }

    private static IResult HandleException(HttpContext context, Exception exception)
    {
        var type = exception.GetType();

        if (ExceptionHandlers.TryGetValue(type, out var handler))
            return handler.Invoke(context, exception);

        // TODO: Testing around unhandled exceptions (https://github.com/SSWConsulting/SSW.CleanArchitecture/issues/80)
        return Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
            type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
    }

    // public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    // {
    //     var type = exception.GetType();
    //
    //     if (ExceptionHandlers.ContainsKey(type))
    //     {
    //         ExceptionHandlers[type].Invoke(httpContext, exception);
    //     }
    //     else
    //     {
    //         // TODO: Testing around unhandled exceptions (https://github.com/SSWConsulting/SSW.CleanArchitecture/issues/80)
    //         Results.Problem(statusCode: StatusCodes.Status500InternalServerError,
    //             type: "https://tools.ietf.org/html/rfc7231#section-6.6.1");
    //     }
    //
    //     return ValueTask.FromResult(true);
    // }

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
}
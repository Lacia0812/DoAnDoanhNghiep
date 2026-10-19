using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace GiaPhuWaterDelivery.Api.Helpers;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (BusinessException ex)
        {
            await WriteProblem(context, ex.StatusCode, ex.Message, ex.Code);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database update failed");
            await WriteProblem(context, StatusCodes.Status409Conflict,
                "Dữ liệu bị trùng hoặc đang được tham chiếu.", "database_conflict");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled request error");
            await WriteProblem(context, StatusCodes.Status500InternalServerError,
                "Đã xảy ra lỗi máy chủ.", "internal_error");
        }
    }

    private static async Task WriteProblem(HttpContext context, int status, string detail, string? code)
    {
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Detail = detail,
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await context.Response.WriteAsJsonAsync(problem);
    }
}

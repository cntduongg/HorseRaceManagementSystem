using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Middlewares;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized", exception.Message),
            InvalidOperationException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            DbUpdateException dbEx => (StatusCodes.Status409Conflict, "Conflict", ResolveDbUpdateDetail(dbEx)),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", exception.Message)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else if (statusCode == StatusCodes.Status409Conflict)
        {
            // Vẫn log chi tiết lỗi DB (kèm inner exception) để chẩn đoán, nhưng trả message gọn cho client.
            _logger.LogWarning(exception, "Database conflict: {Message}", exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    // DbUpdateException.Message chỉ là "An error occurred while saving the entity changes…" — không rõ nguyên nhân.
    // Nếu inner là vi phạm unique constraint của Postgres (SqlState 23505) thì trả message thân thiện, dễ hiểu.
    private static string ResolveDbUpdateDetail(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation }
            ? "Dữ liệu bị trùng với một bản ghi đã tồn tại (vi phạm ràng buộc duy nhất)."
            : "Không thể lưu thay đổi do xung đột dữ liệu.";
}

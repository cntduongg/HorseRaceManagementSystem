using Domain.Common.PhoneNumbers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Api.Middlewares;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string UniquePhoneConstraintName =
        "UX_Users_NormalizedPhoneNumber";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger)
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
            UnauthorizedAccessException =>
                (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    exception.Message
                ),

            KeyNotFoundException =>
                (
                    StatusCodes.Status404NotFound,
                    "Not Found",
                    exception.Message
                ),

            InvalidPhoneNumberException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Invalid Phone Number",
                    "The phone number is not valid."
                ),

            /*
             * Phải đặt trước DbUpdateException chung.
             * Nếu không, lỗi số điện thoại trùng sẽ bị case chung bắt trước.
             */
            DbUpdateException dbException
                when IsDuplicatePhoneNumber(dbException) =>
                (
                    StatusCodes.Status409Conflict,
                    "Phone Number Already Exists",
                    "This phone number is already registered."
                ),

            DbUpdateException dbException =>
                (
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    ResolveDbUpdateDetail(dbException)
                ),

            InvalidOperationException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    exception.Message
                ),

            ArgumentException =>
                (
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    exception.Message
                ),

            _ =>
                (
                    StatusCodes.Status500InternalServerError,
                    "Internal Server Error",
                    "An unexpected error occurred."
                )
        };

        LogException(exception, statusCode);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }

    private void LogException(
        Exception exception,
        int statusCode)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception: {Message}",
                exception.Message);

            return;
        }

        if (statusCode == StatusCodes.Status409Conflict)
        {
            _logger.LogWarning(
                exception,
                "Database conflict: {Message}",
                exception.Message);

            return;
        }

        _logger.LogWarning(
            exception,
            "Request failed with status {StatusCode}: {Message}",
            statusCode,
            exception.Message);
    }

    private static bool IsDuplicatePhoneNumber(
        DbUpdateException exception)
    {
        return exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: UniquePhoneConstraintName
        };
    }

    private static string ResolveDbUpdateDetail(
        DbUpdateException exception)
    {
        if (exception.InnerException is not PostgresException postgresException)
        {
            return "Unable to save changes due to a data conflict.";
        }

        return postgresException.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation =>
                "The data conflicts with an existing record.",

            PostgresErrorCodes.ForeignKeyViolation =>
                "The referenced data does not exist or is currently in use.",

            PostgresErrorCodes.NotNullViolation =>
                "A required value is missing.",

            PostgresErrorCodes.CheckViolation =>
                "The provided data violates a database rule.",

            _ =>
                "Unable to save changes due to a database conflict."
        };
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Api.Filters;

public class ApiResponse<T>
{
    public int StatusCode { get; set; }
    public bool Success { get; set; }
    public T? Data { get; set; }
}

public class ApiResponseFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult objectResult) return;

        var response = new ApiResponse<object>
        {
            StatusCode = objectResult.StatusCode ?? 0,
            Success = objectResult.StatusCode is >= 200 and < 300,
            Data = objectResult.Value,
        };

        context.Result = new ObjectResult(response)
        {
            StatusCode = objectResult.StatusCode ?? 0,
        };
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
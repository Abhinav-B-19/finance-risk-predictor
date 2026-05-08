using System.Net;
using System.Text.Json;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(
        RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch
        {
            context.Response.ContentType =
                "application/json";

            context.Response.StatusCode =
                (int)HttpStatusCode
                    .InternalServerError;

            var response = new
            {
                status = "error",
                message =
                    "Prediction service temporarily unavailable"
            };

            var json =
                JsonSerializer.Serialize(
                    response);

            await context.Response
                .WriteAsync(json);
        }
    }
}
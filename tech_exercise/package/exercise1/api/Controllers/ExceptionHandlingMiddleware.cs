using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;

namespace StargateAPI.Controllers
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.Extensions.Logging.ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, Microsoft.Extensions.Logging.ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);

                // Log successful request with status code and elapsed time
                try
                {
                    sw.Stop();
                    _logger.LogInformation("Request {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds}ms",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode,
                        sw.ElapsedMilliseconds);

                    // Persist to database (best-effort)
                    try
                    {
                        var entry = new StargateAPI.Business.Data.LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Information",
                            Message = string.Empty,
                            Path = context.Request.Path,
                            Method = context.Request.Method,
                            StatusCode = context.Response.StatusCode,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds
                        };
                        var db = context.RequestServices.GetRequiredService<StargateAPI.Business.Data.StargateContext>();
                        await db.Logs.AddAsync(entry, context.RequestAborted);
                        await db.SaveChangesAsync(context.RequestAborted);
                    }
                    catch
                    {
                        // swallow db errors
                    }
                }
                catch
                {
                    // Swallow logging errors to avoid impacting request processing
                }
            }
            catch (Exception ex)
            {
                // Ensure stopwatch is stopped
                try { sw.Stop(); } catch { }

                // Log the exception with stack trace and elapsed time
                try
                {
                    _logger.LogError(ex, "Unhandled exception occurred while processing request in {ElapsedMilliseconds}ms", sw.ElapsedMilliseconds);

                    // Persist error to database (best-effort)
                    try
                    {
                        var entry = new StargateAPI.Business.Data.LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            Level = "Error",
                            Message = ex.Message,
                            Exception = ex.ToString(),
                            Path = context.Request?.Path ?? string.Empty,
                            Method = context.Request?.Method ?? string.Empty,
                            StatusCode = StatusCodes.Status500InternalServerError,
                            ElapsedMilliseconds = sw.ElapsedMilliseconds
                        };
                        var db = context.RequestServices.GetRequiredService<StargateAPI.Business.Data.StargateContext>();
                        await db.Logs.AddAsync(entry, context.RequestAborted);
                        await db.SaveChangesAsync(context.RequestAborted);
                    }
                    catch
                    {
                        // swallow db errors
                    }
                }
                catch
                {
                    // Swallow logging errors
                }

                var response = ExceptionMapper.Map(ex);
                context.Response.StatusCode = response.ResponseCode;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(response);
                await context.Response.WriteAsync(json);
            }
        }
    }
}

using System.Net;
using System.Text.Json;
using backend.DTOs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace backend.Middleware
{
    /**
     * Middleware de Manejo Global de Excepciones
     * Atrapa cualquier error no controlado y lo devuelve en formato ApiResponse.
     */
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocurrió un error inesperado en ISTPET API");
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var detail = _env.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : null;
            if (_env.IsDevelopment() && exception.InnerException != null)
                detail += $" | Inner: {exception.InnerException.Message}";

            var payload = ApiResponse<string>.Fail("Error interno del servidor. Consulte soporte técnico.", detail);
            var result = JsonSerializer.Serialize(payload);
            return context.Response.WriteAsync(result);
        }
    }
}

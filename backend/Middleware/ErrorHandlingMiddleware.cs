using System.Net;
using System.Text.Json;
using backend.DTOs;

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

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
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

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var result = JsonSerializer.Serialize(ApiResponse<string>.Fail("Error interno del servidor. Consulte soporte técnico."));
            return context.Response.WriteAsync(result);
        }
    }
}

using Microsoft.AspNetCore.Http;
using MyWebApi.DTO;
using MyWebApi.Exceptions;
using System.Net;
using System.Text.Json;

// Client Request → ApiResponseMiddleware → Controller → Service → Repository → Database
// Database → Repository → Service → Controller → ApiResponseMiddleware → Client Response
namespace MyWebApi.Middlewares
{
    /// <summary>
    /// Middleware để tự động format API response và xử lý lỗi
    /// </summary>
    public class ApiResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiResponseMiddleware> _logger;
        private static readonly string[] StaticExtensions = { ".html", ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico", ".svg", ".woff", ".woff2", ".ttf", ".eot" };
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public ApiResponseMiddleware(RequestDelegate next, ILogger<ApiResponseMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Kiểm tra xem request có nên được xử lý bởi middleware không
            if (ShouldSkipProcessing(context))
            {
                await _next(context);
                return;
            }

            var originalResponseBodyStream = context.Response.Body;

            try
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                responseBody.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBody).ReadToEndAsync();

                await FormatResponse(context, responseContent, originalResponseBodyStream);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, originalResponseBodyStream);
            }
            finally
            {
                context.Response.Body = originalResponseBodyStream;
            }
        }

        /// Kiểm tra xem request có nên được bỏ qua xử lý bởi middleware không
        private static bool ShouldSkipProcessing(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Bỏ qua Swagger và static files
            if (path.StartsWith("/swagger") || StaticExtensions.Any(path.EndsWith))
                return true;

            // Bỏ qua non-JSON content types
            var acceptHeader = context.Request.Headers["Accept"].ToString().ToLower();
            if (acceptHeader.Contains("text/html") || acceptHeader.Contains("image/") || acceptHeader.Contains("font/"))
                return true;

            // Chỉ xử lý API requests
            return !(path.StartsWith("/api") || acceptHeader.Contains("application/json") ||
                    context.Request.ContentType?.Contains("application/json") == true ||
                    context.Request.Method != "GET");
        }

  
        /// Format lại response thành chuẩn ApiResponse
  
        private async Task FormatResponse(HttpContext context, string responseContent, Stream originalStream)
        {
            context.Response.Body = originalStream;

            var statusCode = context.Response.StatusCode;
            var contentType = context.Response.ContentType?.ToLower() ?? "";

            // Giữ nguyên non-JSON responses
            if (!string.IsNullOrEmpty(contentType) && !contentType.Contains("application/json"))
            {
                await context.Response.WriteAsync(responseContent);
                return;
            }

            // Không format lại nếu đã là ApiResponse
            if (IsApiResponseFormat(responseContent))
            {
                await WriteJsonResponse(context, responseContent);
                return;
            }

            var apiResponse = CreateApiResponse(statusCode, responseContent);
            var jsonResponse = JsonSerializer.Serialize(apiResponse, JsonOptions);
            await WriteJsonResponse(context, jsonResponse);
        }


        /// Xử lý exception và trả về response lỗi
        private async Task HandleExceptionAsync(HttpContext context, Exception exception, Stream originalStream)
        {
            context.Response.Body = originalStream;
            _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

            var (statusCode, message, errors) = GetErrorDetails(exception);
            context.Response.StatusCode = statusCode;

            var apiResponse = new ApiResponse<object>
            {
                StatusCode = statusCode,
                Message = message,
                Data = null,
                Errors = errors
            };

            var jsonResponse = JsonSerializer.Serialize(apiResponse, JsonOptions);
            await WriteJsonResponse(context, jsonResponse);
        }

        /// Tạo ApiResponse dựa trên status code

        private ApiResponse<object> CreateApiResponse(int statusCode, string responseContent)
        {
            if (statusCode >= 200 && statusCode < 300)
            {
                return new ApiResponse<object>
                {
                    StatusCode = statusCode,
                    Message = GetSuccessMessage(statusCode),
                    Data = DeserializeContent(responseContent),
                    Errors = new List<string>()
                };
            }

            return new ApiResponse<object>
            {
                StatusCode = statusCode,
                Message = GetErrorMessage(statusCode),
                Data = null,
                Errors = new List<string> { responseContent }
            };
        }

   
        /// Lấy chi tiết lỗi từ exception
     
        private static (int statusCode, string message, List<string> errors) GetErrorDetails(Exception exception)
        {
            return exception switch
            {
                NotFoundException ex => (404, "Not Found", new List<string> { ex.Message }),
                ValidationException ex => (400, "Validation Failed", ex.Errors.ToList()),
                BusinessException ex => (400, "Business Logic Error", new List<string> { ex.Message }),
                ArgumentNullException => (400, "Bad Request", new List<string> { "Required parameter is missing" }),
                ArgumentException ex => (400, "Bad Request", new List<string> { ex.Message }),
                UnauthorizedAccessException => (401, "Unauthorized", new List<string> { "Access denied" }),
                NotImplementedException => (501, "Not Implemented", new List<string> { "Feature not implemented" }),
                _ => (500, "Internal Server Error", new List<string> { "An unexpected error occurred" })
            };
        }

        /// Kiểm tra xem response đã có format ApiResponse chưa
     
        private static bool IsApiResponseFormat(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;

            try
            {
                using var jsonDoc = JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;
                return root.TryGetProperty("statusCode", out _) && root.TryGetProperty("message", out _) &&
                       root.TryGetProperty("data", out _) && root.TryGetProperty("errors", out _);
            }
            catch { return false; }
        }

        /// Deserialize response content
        /// </summary>
        private static object? DeserializeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return null;

            try { return JsonSerializer.Deserialize<object>(content); }
            catch { return content; }
        }

        /// <summary>
        /// Lấy success message theo status code
        /// </summary>
        private static string GetSuccessMessage(int statusCode) => statusCode switch
        {
            201 => "Created successfully",
            204 => "No content",
            _ => "Success"
        };

        /// <summary>
        /// Lấy error message theo status code
        /// </summary>
        private static string GetErrorMessage(int statusCode) => statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            409 => "Conflict",
            500 => "Internal Server Error",
            _ => "Error"
        };

        /// <summary>
        /// Viết JSON response
        /// </summary>
        private static async Task WriteJsonResponse(HttpContext context, string content)
        {
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(content);
        }
    }

    /// <summary>
    /// Extension methods để đăng ký middleware
    /// </summary>
    public static class ApiResponseMiddlewareExtensions
    {
        /// <summary>
        /// Thêm ApiResponseMiddleware vào pipeline
        /// </summary>
        public static IApplicationBuilder UseApiResponseMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<ApiResponseMiddleware>();

        /// <summary>
        /// Thêm ApiResponseMiddleware chỉ cho các API routes cụ thể
        /// </summary>
        public static IApplicationBuilder UseApiResponseMiddleware(this IApplicationBuilder builder, string pathPrefix)
            => builder.UseWhen(
                context => context.Request.Path.StartsWithSegments(pathPrefix),
                appBuilder => appBuilder.UseMiddleware<ApiResponseMiddleware>());
    }
}
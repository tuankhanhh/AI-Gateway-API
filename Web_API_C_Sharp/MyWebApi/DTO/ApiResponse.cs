namespace MyWebApi.DTO
{
    public class ApiResponse<T>
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public IEnumerable<string> Errors { get; set; } = new List<string>();

        /// Tạo response thành công
        public static ApiResponse<T> Success(T data, string message = "Success", int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = data,
                Errors = new List<string>()
            };
        }

        /// Tạo response lỗi
        public static ApiResponse<T> Error(string message, int statusCode = 400, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = default,
                Errors = errors ?? new List<string> { message }
            };
        }

        /// Tạo response lỗi với nhiều lỗi
        public static ApiResponse<T> Error(IEnumerable<string> errors, string message = "Validation failed", int statusCode = 400)
        {
            return new ApiResponse<T>
            {
                StatusCode = statusCode,
                Message = message,
                Data = default,
                Errors = errors
            };
        }
    }
}

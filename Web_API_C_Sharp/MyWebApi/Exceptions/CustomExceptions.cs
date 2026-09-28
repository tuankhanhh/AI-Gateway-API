namespace MyWebApi.Exceptions
{
    /// <summary>
    /// Exception cho lỗi không tìm thấy resource
    /// </summary>
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) { }
        public NotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception cho lỗi business logic
    /// </summary>
    public class BusinessException : Exception
    {
        public BusinessException(string message) : base(message) { }
        public BusinessException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception cho lỗi validation
    /// </summary>
    public class ValidationException : Exception
    {
        public List<string> Errors { get; }

        public ValidationException(string message) : base(message)
        {
            Errors = new List<string> { message };
        }

        public ValidationException(List<string> errors) : base("Validation failed")
        {
            Errors = errors;
        }
    }

    public class AiProviderException : Exception
    {
        public int StatusCode { get; }
        public string ErrorCode { get; }

        public AiProviderException(int statusCode, string errorCode, string message) : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
        }
    }

    public class RateLimitException : Exception
    {
        public RateLimitException(string message) : base(message) { }
    }
}

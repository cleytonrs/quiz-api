namespace QuizApp.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 500) : base(message)
    {
        StatusCode = statusCode;
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409)
    {
    }
}

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message) : base(message, 401)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404)
    {
    }
}

public class ValidationException : AppException
{
    public string[] Details { get; }

    public ValidationException(string message, string[]? details = null) : base(message, 400)
    {
        Details = details ?? Array.Empty<string>();
    }
}

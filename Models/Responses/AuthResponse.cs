namespace lockhaven_backend.Models.Responses;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public UserResponse User { get; set; } = new ();

    public static AuthResponse SuccessResponse(string message, string token, UserResponse user)
    {
        return new AuthResponse
        {
            Success = true,
            Message = message,
            Token = token,
            User = user
        };
    }

    public static ProblemResponse FailureResponse(string message, int statusCode = 400)
    {
        return ProblemResponse.From(
            title: message,
            detail: message,
            statusCode: statusCode
        );
    }
}
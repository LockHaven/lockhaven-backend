using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace lockhaven_backend.Models.Responses;

/// <summary>
/// Represents a standardized problem-style response (based on RFC 7807)
/// for both expected and unexpected API failures.
/// </summary>
public class ProblemResponse : ProblemDetails
{
    /// <summary>
    /// Indicates whether the operation succeeded. Always false for ProblemResponse.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success => false;

    /// <summary>
    /// A map of field-specific validation errors (optional).
    /// </summary>
    [JsonPropertyName("errors")]
    public IDictionary<string, string[]>? Errors { get; set; }

    public static ProblemResponse From(
        string title,
        string detail,
        int statusCode = 400,
        IDictionary<string, string[]>? errors = null)
    {
        return new ProblemResponse
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Errors = errors
        };
    }
}

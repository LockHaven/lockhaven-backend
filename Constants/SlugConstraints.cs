using System.Text.RegularExpressions;

namespace lockhaven_backend.Constants;

/// <summary>
/// URL-safe slug rules: lowercase alphanumeric segments separated by single hyphens (kebab-case).
/// Stored values are always normalized with <see cref="NormalizeSlug"/>.
/// </summary>
public static class SlugConstraints
{
    /// <summary>Strict pattern for persisted slugs (lowercase only).</summary>
    public const string Pattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";

    /// <summary>Allows empty string for optional slug fields on update DTOs.</summary>
    public const string OptionalPattern = @"^([a-z0-9]+(?:-[a-z0-9]+)*)?$";

    public const string PatternDescription = "lowercase letters, digits, and single hyphens between segments (e.g. my-app-2)";

    private static readonly Regex Regex = new(Pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string NormalizeSlug(string? slug) => slug?.Trim().ToLowerInvariant() ?? string.Empty;

    public static bool IsValid(string normalizedSlug)
    {
        if (string.IsNullOrEmpty(normalizedSlug))
        {
            return false;
        }

        return Regex.IsMatch(normalizedSlug);
    }
}

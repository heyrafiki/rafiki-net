using System.Text.RegularExpressions;

namespace Heyrafiki.Internal;

internal static class Guard
{
    private static readonly Regex IdentifierCharacters = new("^[A-Za-z0-9_-]+$", RegexOptions.CultureInvariant);
    private static readonly Regex ArtifactReferenceCharacters = new("^[A-Za-z0-9][A-Za-z0-9:._/-]*$", RegexOptions.CultureInvariant);

    internal static string Identifier(string value, string prefix, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 ||
            !value.StartsWith(prefix, StringComparison.Ordinal) ||
#if NETSTANDARD2_0
            !IdentifierCharacters.IsMatch(value.Substring(prefix.Length)))
#else
            !IdentifierCharacters.IsMatch(value.AsSpan(prefix.Length)))
#endif
            throw new ArgumentException($"{parameterName} must be a valid {prefix.TrimEnd('_')} identifier.", parameterName);
        return value;
    }

    internal static int Limit(int limit)
    {
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit), "limit must be between 1 and 100.");
        return limit;
    }

    internal static string IdempotencyKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length is < 8 or > 255)
            throw new ArgumentException("idempotencyKey must contain between 8 and 255 characters.", nameof(value));
        return value;
    }

    internal static string ArtifactReference(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 190 || !ArtifactReferenceCharacters.IsMatch(value))
            throw new ArgumentException("artifactReference is not valid.", nameof(value));
        return value;
    }

    internal static T Input<T>(T value, string parameterName) where T : class =>
        value ?? throw new ArgumentNullException(parameterName);

    internal static string Escape(string value) => Uri.EscapeDataString(value);
}

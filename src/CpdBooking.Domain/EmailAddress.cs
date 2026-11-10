namespace CpdBooking.Domain;

/// <summary>
/// A normalised email address. Value object: two addresses that differ only in case or
/// surrounding spaces are the same address.
/// </summary>
public sealed record EmailAddress
{
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static bool TryCreate(string? input, out EmailAddress? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var candidate = input.Trim().ToLowerInvariant();
        if (candidate.Length > MaxLength || candidate.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = candidate.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != candidate.LastIndexOf('@') || at == candidate.Length - 1)
        {
            return false;
        }

        var domain = candidate[(at + 1)..];
        if (!domain.Contains('.', StringComparison.Ordinal) || domain.StartsWith('.') || domain.EndsWith('.'))
        {
            return false;
        }

        email = new EmailAddress(candidate);
        return true;
    }

    public override string ToString() => Value;
}

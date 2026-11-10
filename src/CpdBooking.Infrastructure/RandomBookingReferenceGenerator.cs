using System.Security.Cryptography;
using CpdBooking.Domain;

namespace CpdBooking.Infrastructure;

/// <summary>
/// Creates references like CPD-7KQ4-MX2P from a cryptographic random source, so they can't be
/// guessed (an attacker can't walk through other people's bookings).
/// </summary>
public sealed class RandomBookingReferenceGenerator : IBookingReferenceGenerator
{
    // No 0/O or 1/I/L, so references are easy to read out over the phone.
    private const string Alphabet = "23456789ABCDEFGHJKMNPQRSTUVWXYZ";

    public string Create()
    {
        Span<char> chars = stackalloc char[8];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return $"CPD-{chars[..4]}-{chars[4..]}";
    }
}

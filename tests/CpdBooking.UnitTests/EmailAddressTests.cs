using System.Net.Mail;
using CpdBooking.Domain;

namespace CpdBooking.UnitTests;

public class EmailAddressTests
{
    [Fact]
    public void An_email_is_trimmed_and_lower_cased()
    {
        Assert.True(EmailAddress.TryCreate(" Teacher@School.ORG.uk ", out var email));
        Assert.Equal("teacher@school.org.uk", email!.Value);
    }

    [Fact]
    public void An_email_longer_than_254_characters_is_rejected()
    {
        var tooLong = new string('a', 250) + "@b.co";
        Assert.False(EmailAddress.TryCreate(tooLong, out _));
    }

    [Fact]
    public void Two_addresses_differing_only_in_case_are_equal()
    {
        EmailAddress.TryCreate("A@B.com", out var first);
        EmailAddress.TryCreate("a@b.COM", out var second);
        Assert.Equal(first, second);
    }

}

namespace CpdBooking.Domain;

/// <summary>Creates booking references. An abstraction so tests can use predictable values.</summary>
public interface IBookingReferenceGenerator
{
    string Create();
}

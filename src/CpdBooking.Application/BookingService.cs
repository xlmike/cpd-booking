using CpdBooking.Domain;
using Microsoft.Extensions.Logging;

namespace CpdBooking.Application;

/// <summary>Write side: booking a place. Orchestrates the domain and persistence; the rules live in <see cref="Course"/>.</summary>
public sealed partial class BookingService(
    ICourseRepository courses,
    IBookingReferenceGenerator references,
    TimeProvider clock,
    ILogger<BookingService> logger)
{
    public const int MaxAttempts = 3;

    public async Task<BookingResult> BookAsync(Guid courseId, string? email, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var course = await courses.FindAsync(courseId, cancellationToken).ConfigureAwait(false);
            if (course is null)
            {
                return BookingResult.Failure(BookingError.CourseNotFound);
            }

            var result = course.Book(email, clock.GetUtcNow(), references);
            if (!result.IsSuccess)
            {
                LogBookingRejected(logger, courseId, result.Error);
                return result;
            }

            try
            {
                await courses.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                // Log the reference, never the email address (personal data).
                LogBookingCreated(logger, result.Booking!.Reference, courseId);
                return result;
            }
            catch (DuplicateBookingException)
            {
                courses.DiscardChanges();
                return BookingResult.Failure(BookingError.AlreadyBooked);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
                LogConcurrencyRetry(logger, courseId, attempt);
                courses.DiscardChanges();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking {Reference} created for course {CourseId}")]
    private static partial void LogBookingCreated(ILogger logger, string reference, Guid courseId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Booking rejected for course {CourseId}: {Error}")]
    private static partial void LogBookingRejected(ILogger logger, Guid courseId, BookingError error);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Concurrency conflict booking course {CourseId}, attempt {Attempt}; retrying")]
    private static partial void LogConcurrencyRetry(ILogger logger, Guid courseId, int attempt);
}

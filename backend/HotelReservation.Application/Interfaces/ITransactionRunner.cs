namespace HotelReservation.Application.Interfaces;

/// <summary>
/// Runs a unit of work inside a single serializable database transaction, so multiple
/// repository calls made within it (e.g. a check followed by an insert) happen
/// atomically -- no other transaction can commit a conflicting change in between.
/// Exists so use cases that need that guarantee (see <c>CreateReservation</c>) don't
/// have to depend on EF Core's transaction APIs directly, keeping this project's
/// Application layer free of infrastructure concerns.
/// </summary>
/// <remarks>
/// A genuine conflict from a concurrently-committed transaction is translated by the
/// implementation into a <see cref="Common.Exceptions.ConflictException"/> -- the same
/// exception an application-level "already booked" rejection throws -- so callers
/// don't need to know or care which kind of conflict it was.
/// </remarks>
public interface ITransactionRunner
{
    Task<T> RunSerializableAsync<T>(Func<Task<T>> operation);
}

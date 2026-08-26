using System.Data;
using HotelReservation.Application.Common.Exceptions;
using HotelReservation.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelReservation.Infrastructure.Persistence;

/// <summary>
/// EF Core-backed <see cref="ITransactionRunner"/>: wraps a unit of work in a single
/// <see cref="IsolationLevel.Serializable"/> transaction on <see cref="HotelDbContext"/>.
/// </summary>
/// <remarks>
/// On SQL Server, Serializable isolation range-locks the rows a transaction reads, so
/// two requests racing to book the same room/dates can no longer both pass the overlap
/// check and both insert -- SQL Server aborts the losing side outright instead (a
/// deadlock victim, or a serialization-failure error) rather than silently letting both
/// through. SQLite has no separate isolation levels of its own (its single-writer
/// locking model already serializes writers), so requesting Serializable there doesn't
/// add anything beyond what SQLite already guarantees -- see docs/decisions.md for why
/// that's accepted rather than requiring a real SQL Server instance just to test this.
/// </remarks>
public class TransactionRunner : ITransactionRunner
{
    private readonly HotelDbContext _context;
    private readonly ILogger<TransactionRunner> _logger;

    public TransactionRunner(HotelDbContext context, ILogger<TransactionRunner> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// See <see cref="ITransactionRunner.RunSerializableAsync{T}"/>.
    /// </summary>
    public async Task<T> RunSerializableAsync<T>(Func<Task<T>> operation)
    {
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction;
        try
        {
            transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        }
        catch (Exception ex)
        {
            // In production (SQL Server, a fresh pooled connection per request) this
            // realistically never happens. It does happen in Tests.Integration, though:
            // every request there shares one physical SqliteConnection (see
            // CustomWebApplicationFactory), so SQLite itself rejects a second BEGIN on a
            // connection that already has an active transaction -- exactly what two
            // concurrent requests racing for the same room produce (verified: this
            // throws Microsoft.Data.Sqlite.SqliteException, error 1, "SQL logic error").
            // Either way, failing to even open this transaction means this request
            // can't proceed safely right now -- "conflict, please retry" is the right
            // response either way; see docs/decisions.md.
            _logger.LogWarning(ex, "Could not start transaction; treating as a booking conflict.");
            throw new ConflictException("Room is already reserved for this period.");
        }

        await using (transaction)
        {
            try
            {
                var result = await operation();
                await transaction.CommitAsync();
                return result;
            }
            catch (DbUpdateException ex)
            {
                // This transaction only ever runs an overlap check plus a single
                // insert, so a DbUpdateException surfacing here means either SQL
                // Server aborted this transaction for conflicting with another one
                // (the case this exists for), or -- rarely -- the room disappeared
                // concurrently underneath it. Either way, "conflict, please retry" is
                // the right response to the caller; not retried automatically here,
                // see docs/decisions.md. The original exception is logged (not
                // attached to ConflictException, which -- like every AppException --
                // is message-only by convention) so the real cause is still visible
                // server-side.
                _logger.LogWarning(ex, "Transaction aborted; treating as a booking conflict.");
                throw new ConflictException("Room is already reserved for this period.");
            }
        }
    }
}

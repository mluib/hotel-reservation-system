using HotelReservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        // DateRange maps onto the existing CheckIn/CheckOut columns -- shape change only,
        // no schema change.
        builder.OwnsOne(r => r.Stay, stay =>
        {
            stay.Property(s => s.CheckIn).HasColumnName("CheckIn");
            stay.Property(s => s.CheckOut).HasColumnName("CheckOut");
        });

        // Same Money story as RoomConfiguration: Amount reuses the existing PricePerNight
        // column, Currency is a new column backfilled by the migration.
        builder.OwnsOne(r => r.PricePerNight, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerNight")
                .HasColumnType("decimal(18,2)");

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired()
                // Same reasoning as RoomConfiguration: a real backfill value, not EF's
                // own empty-string default for a non-nullable column.
                .HasDefaultValue("EUR");
        });

        // Room/Customer no longer have a Reservations collection navigation (Phase 6
        // aggregate cleanup: Room, Customer, Reservation are independent aggregate roots,
        // referencing each other only by id), so EF's convention-based FK inference -- which
        // relied entirely on those navigations -- no longer applies. Configured explicitly
        // here instead, on Restrict rather than the previous convention-inferred Cascade:
        // the application layer already rejects deleting a room/customer with reservations
        // (ConflictException in DeleteRoom/DeleteCustomer), so this makes the database enforce
        // the same rule as defense-in-depth, rather than silently cascading reservation
        // history away if that guard is ever bypassed.
        builder.HasOne<Domain.Entities.Room>()
            .WithMany()
            .HasForeignKey(r => r.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.Customer>()
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

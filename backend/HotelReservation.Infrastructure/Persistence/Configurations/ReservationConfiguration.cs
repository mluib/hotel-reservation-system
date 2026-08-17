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
    }
}

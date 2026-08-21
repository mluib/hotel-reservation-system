using HotelReservation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.Property(r => r.Name)
            .HasMaxLength(60)
            .IsRequired()
            // Backfills existing rows with a generic-but-non-blank placeholder instead of
            // EF's own empty-string default for a new non-nullable column, same reasoning
            // as Currency below -- an empty Name would render as a blank in the frontend.
            .HasDefaultValue("Room");

        // Money has two properties (Amount, Currency), so it needs an owned entity rather
        // than a single-column ValueConverter -- Amount reuses the existing PricePerNight
        // column, Currency is a genuinely new column (migration backfills existing rows).
        builder.OwnsOne(r => r.PricePerNight, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("PricePerNight")
                .HasColumnType("decimal(18,2)");

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsRequired()
                // Backfills existing rows with a real 3-letter code instead of EF's own
                // empty-string default for a non-nullable column, which would violate
                // Money's own constructor invariant the moment such a row is read back.
                .HasDefaultValue("EUR");
        });
    }
}

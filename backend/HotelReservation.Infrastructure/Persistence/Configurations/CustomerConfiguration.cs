using HotelReservation.Domain.Entities;
using HotelReservation.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelReservation.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // EmailAddress is a single scalar (just Value), so a ValueConverter is enough --
        // no schema change, it maps onto the existing Email column unchanged.
        builder.Property(c => c.Email)
            .HasConversion(email => email.Value, value => new EmailAddress(value))
            .HasColumnName("Email");
    }
}

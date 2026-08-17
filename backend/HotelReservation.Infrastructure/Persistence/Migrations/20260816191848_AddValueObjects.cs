using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HotelReservation.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddValueObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Rooms",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Reservations",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Reservations");
        }
    }
}

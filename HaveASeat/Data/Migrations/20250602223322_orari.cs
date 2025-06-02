using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class orari : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDayOff",
                table: "Orario",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDayOff",
                table: "Orario");
        }
    }
}

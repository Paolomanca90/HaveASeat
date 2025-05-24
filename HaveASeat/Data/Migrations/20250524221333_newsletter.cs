using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class newsletter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AcceptNewsletter",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptNewsletter",
                table: "AspNetUsers");
        }
    }
}

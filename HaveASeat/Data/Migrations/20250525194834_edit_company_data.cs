using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class edit_company_data : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Azienda",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PartitaIVA",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RagioneSociale",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SDI",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "PartitaIVA",
                table: "Salone",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RagioneSociale",
                table: "Salone",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SDI",
                table: "Salone",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PartitaIVA",
                table: "Salone");

            migrationBuilder.DropColumn(
                name: "RagioneSociale",
                table: "Salone");

            migrationBuilder.DropColumn(
                name: "SDI",
                table: "Salone");

            migrationBuilder.AddColumn<bool>(
                name: "Azienda",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PartitaIVA",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RagioneSociale",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SDI",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

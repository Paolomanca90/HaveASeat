using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class fix_appuntamenti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ServizioId",
                table: "Appuntamento",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_ServizioId",
                table: "Appuntamento",
                column: "ServizioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appuntamento_Servizio_ServizioId",
                table: "Appuntamento",
                column: "ServizioId",
                principalTable: "Servizio",
                principalColumn: "ServizioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appuntamento_Servizio_ServizioId",
                table: "Appuntamento");

            migrationBuilder.DropIndex(
                name: "IX_Appuntamento_ServizioId",
                table: "Appuntamento");

            migrationBuilder.DropColumn(
                name: "ServizioId",
                table: "Appuntamento");
        }
    }
}

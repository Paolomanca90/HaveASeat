using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class new_tabella : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PianoSelezionato",
                columns: table => new
                {
                    PianoSelezioantoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AbbonamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Confermato = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PianoSelezionato");
        }
    }
}

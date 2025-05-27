using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixPianoSelezionatoKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PianoSelezioantoId",
                table: "PianoSelezionato",
                newName: "PianoSelezionatoId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PianoSelezionato",
                table: "PianoSelezionato",
                column: "PianoSelezionatoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PianoSelezionato",
                table: "PianoSelezionato");

            migrationBuilder.RenameColumn(
                name: "PianoSelezionatoId",
                table: "PianoSelezionato",
                newName: "PianoSelezioantoId");
        }
    }
}

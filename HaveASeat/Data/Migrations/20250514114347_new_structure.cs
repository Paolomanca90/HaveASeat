using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class new_structure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appuntamento_Salone_SaloneId1",
                table: "Appuntamento");

            migrationBuilder.DropForeignKey(
                name: "FK_Appuntamento_Slot_SlotId1",
                table: "Appuntamento");

            migrationBuilder.DropForeignKey(
                name: "FK_Recensione_Salone_SaloneId1",
                table: "Recensione");

            migrationBuilder.DropForeignKey(
                name: "FK_SaloneAbbonamento_Salone_SaloneId1",
                table: "SaloneAbbonamento");

            migrationBuilder.DropForeignKey(
                name: "FK_Servizio_Salone_SaloneId",
                table: "Servizio");

            migrationBuilder.DropTable(
                name: "SaloneResponsabile");

            migrationBuilder.DropTable(
                name: "SaloneServizio");

            migrationBuilder.DropIndex(
                name: "IX_SaloneAbbonamento_SaloneId1",
                table: "SaloneAbbonamento");

            migrationBuilder.DropIndex(
                name: "IX_Appuntamento_SaloneId1",
                table: "Appuntamento");

            migrationBuilder.DropColumn(
                name: "SaloneId1",
                table: "SaloneAbbonamento");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OraFine",
                table: "Appuntamento");

            migrationBuilder.DropColumn(
                name: "OraInizio",
                table: "Appuntamento");

            migrationBuilder.DropColumn(
                name: "SaloneId1",
                table: "Appuntamento");

            migrationBuilder.RenameColumn(
                name: "IsAvailable",
                table: "Slot",
                newName: "IsAttivo");

            migrationBuilder.RenameColumn(
                name: "SaloneId1",
                table: "Recensione",
                newName: "DipendenteId");

            migrationBuilder.RenameIndex(
                name: "IX_Recensione_SaloneId1",
                table: "Recensione",
                newName: "IX_Recensione_DipendenteId");

            migrationBuilder.RenameColumn(
                name: "SlotId1",
                table: "Appuntamento",
                newName: "DipendenteId");

            migrationBuilder.RenameIndex(
                name: "IX_Appuntamento_SlotId1",
                table: "Appuntamento",
                newName: "IX_Appuntamento_DipendenteId");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OraInizio",
                table: "Slot",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<TimeSpan>(
                name: "OraFine",
                table: "Slot",
                type: "time",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<int>(
                name: "Capacita",
                table: "Slot",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GiorniSettimana",
                table: "Slot",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaloneId",
                table: "Servizio",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataFinePromozione",
                table: "Servizio",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DataInizioPromozione",
                table: "Servizio",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsPromotion",
                table: "Servizio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "Giorno",
                table: "Orario",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "IsLogo",
                table: "Immagine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataRegistrazione",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "Azienda",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Categoria",
                columns: table => new
                {
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categoria", x => x.CategoriaId);
                });

            migrationBuilder.CreateTable(
                name: "Dipendente",
                columns: table => new
                {
                    DipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dipendente", x => x.DipendenteId);
                    table.ForeignKey(
                        name: "FK_Dipendente_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dipendente_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "SaloneCategoria",
                columns: table => new
                {
                    SaloneCategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaloneCategoria", x => x.SaloneCategoriaId);
                    table.ForeignKey(
                        name: "FK_SaloneCategoria_Categoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "Categoria",
                        principalColumn: "CategoriaId");
                    table.ForeignKey(
                        name: "FK_SaloneCategoria_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "DipendenteServizio",
                columns: table => new
                {
                    DipendenteServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DipendenteServizio", x => x.DipendenteServizioId);
                    table.ForeignKey(
                        name: "FK_DipendenteServizio_Dipendente_DipendenteId",
                        column: x => x.DipendenteId,
                        principalTable: "Dipendente",
                        principalColumn: "DipendenteId");
                    table.ForeignKey(
                        name: "FK_DipendenteServizio_Servizio_ServizioId",
                        column: x => x.ServizioId,
                        principalTable: "Servizio",
                        principalColumn: "ServizioId");
                });

            migrationBuilder.CreateTable(
                name: "OrarioDipendente",
                columns: table => new
                {
                    OrarioDipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Giorno = table.Column<int>(type: "int", nullable: false),
                    Apertura = table.Column<TimeSpan>(type: "time", nullable: false),
                    Chiusura = table.Column<TimeSpan>(type: "time", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDayOff = table.Column<bool>(type: "bit", nullable: false),
                    InizioFerie = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FineFerie = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrarioDipendente", x => x.OrarioDipendenteId);
                    table.ForeignKey(
                        name: "FK_OrarioDipendente_Dipendente_DipendenteId",
                        column: x => x.DipendenteId,
                        principalTable: "Dipendente",
                        principalColumn: "DipendenteId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dipendente_ApplicationUserId",
                table: "Dipendente",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Dipendente_SaloneId",
                table: "Dipendente",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_DipendenteServizio_DipendenteId",
                table: "DipendenteServizio",
                column: "DipendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_DipendenteServizio_ServizioId",
                table: "DipendenteServizio",
                column: "ServizioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrarioDipendente_DipendenteId",
                table: "OrarioDipendente",
                column: "DipendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneCategoria_CategoriaId",
                table: "SaloneCategoria",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneCategoria_SaloneId",
                table: "SaloneCategoria",
                column: "SaloneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appuntamento_Dipendente_DipendenteId",
                table: "Appuntamento",
                column: "DipendenteId",
                principalTable: "Dipendente",
                principalColumn: "DipendenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recensione_Dipendente_DipendenteId",
                table: "Recensione",
                column: "DipendenteId",
                principalTable: "Dipendente",
                principalColumn: "DipendenteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servizio_Salone_SaloneId",
                table: "Servizio",
                column: "SaloneId",
                principalTable: "Salone",
                principalColumn: "SaloneId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appuntamento_Dipendente_DipendenteId",
                table: "Appuntamento");

            migrationBuilder.DropForeignKey(
                name: "FK_Recensione_Dipendente_DipendenteId",
                table: "Recensione");

            migrationBuilder.DropForeignKey(
                name: "FK_Servizio_Salone_SaloneId",
                table: "Servizio");

            migrationBuilder.DropTable(
                name: "DipendenteServizio");

            migrationBuilder.DropTable(
                name: "OrarioDipendente");

            migrationBuilder.DropTable(
                name: "SaloneCategoria");

            migrationBuilder.DropTable(
                name: "Dipendente");

            migrationBuilder.DropTable(
                name: "Categoria");

            migrationBuilder.DropColumn(
                name: "Capacita",
                table: "Slot");

            migrationBuilder.DropColumn(
                name: "GiorniSettimana",
                table: "Slot");

            migrationBuilder.DropColumn(
                name: "DataFinePromozione",
                table: "Servizio");

            migrationBuilder.DropColumn(
                name: "DataInizioPromozione",
                table: "Servizio");

            migrationBuilder.DropColumn(
                name: "IsPromotion",
                table: "Servizio");

            migrationBuilder.DropColumn(
                name: "IsLogo",
                table: "Immagine");

            migrationBuilder.RenameColumn(
                name: "IsAttivo",
                table: "Slot",
                newName: "IsAvailable");

            migrationBuilder.RenameColumn(
                name: "DipendenteId",
                table: "Recensione",
                newName: "SaloneId1");

            migrationBuilder.RenameIndex(
                name: "IX_Recensione_DipendenteId",
                table: "Recensione",
                newName: "IX_Recensione_SaloneId1");

            migrationBuilder.RenameColumn(
                name: "DipendenteId",
                table: "Appuntamento",
                newName: "SlotId1");

            migrationBuilder.RenameIndex(
                name: "IX_Appuntamento_DipendenteId",
                table: "Appuntamento",
                newName: "IX_Appuntamento_SlotId1");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OraInizio",
                table: "Slot",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<DateTime>(
                name: "OraFine",
                table: "Slot",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(TimeSpan),
                oldType: "time");

            migrationBuilder.AlterColumn<Guid>(
                name: "SaloneId",
                table: "Servizio",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SaloneId1",
                table: "SaloneAbbonamento",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Giorno",
                table: "Orario",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataRegistrazione",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "Azienda",
                table: "AspNetUsers",
                type: "bit",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OraFine",
                table: "Appuntamento",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "OraInizio",
                table: "Appuntamento",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "SaloneId1",
                table: "Appuntamento",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SaloneResponsabile",
                columns: table => new
                {
                    SaloneResponsabileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaloneResponsabile", x => x.SaloneResponsabileId);
                    table.ForeignKey(
                        name: "FK_SaloneResponsabile_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SaloneResponsabile_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "SaloneServizio",
                columns: table => new
                {
                    SaloneServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaloneServizio", x => x.SaloneServizioId);
                    table.ForeignKey(
                        name: "FK_SaloneServizio_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_SaloneServizio_Servizio_ServizioId",
                        column: x => x.ServizioId,
                        principalTable: "Servizio",
                        principalColumn: "ServizioId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaloneAbbonamento_SaloneId1",
                table: "SaloneAbbonamento",
                column: "SaloneId1");

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_SaloneId1",
                table: "Appuntamento",
                column: "SaloneId1");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneResponsabile_ApplicationUserId",
                table: "SaloneResponsabile",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneResponsabile_SaloneId",
                table: "SaloneResponsabile",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneServizio_SaloneId",
                table: "SaloneServizio",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneServizio_ServizioId",
                table: "SaloneServizio",
                column: "ServizioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appuntamento_Salone_SaloneId1",
                table: "Appuntamento",
                column: "SaloneId1",
                principalTable: "Salone",
                principalColumn: "SaloneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appuntamento_Slot_SlotId1",
                table: "Appuntamento",
                column: "SlotId1",
                principalTable: "Slot",
                principalColumn: "SlotId");

            migrationBuilder.AddForeignKey(
                name: "FK_Recensione_Salone_SaloneId1",
                table: "Recensione",
                column: "SaloneId1",
                principalTable: "Salone",
                principalColumn: "SaloneId");

            migrationBuilder.AddForeignKey(
                name: "FK_SaloneAbbonamento_Salone_SaloneId1",
                table: "SaloneAbbonamento",
                column: "SaloneId1",
                principalTable: "Salone",
                principalColumn: "SaloneId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servizio_Salone_SaloneId",
                table: "Servizio",
                column: "SaloneId",
                principalTable: "Salone",
                principalColumn: "SaloneId");
        }
    }
}

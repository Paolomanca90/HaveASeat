using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Azienda",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CAP",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Città",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodiceFiscale",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cognome",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DataRegistrazione",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Indirizzo",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Nome",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartitaIVA",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Provincia",
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

            migrationBuilder.CreateTable(
                name: "Abbonamento",
                columns: table => new
                {
                    AbbonamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezzo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Durata = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Abbonamento", x => x.AbbonamentoId);
                });

            migrationBuilder.CreateTable(
                name: "Salone",
                columns: table => new
                {
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Indirizzo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CAP = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Citta = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Provincia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Regione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SitoWeb = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Salone", x => x.SaloneId);
                    table.ForeignKey(
                        name: "FK_Salone_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Immagine",
                columns: table => new
                {
                    ImmagineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Percorso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Immagine", x => x.ImmagineId);
                    table.ForeignKey(
                        name: "FK_Immagine_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "Orario",
                columns: table => new
                {
                    OrarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Giorno = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apertura = table.Column<TimeSpan>(type: "time", nullable: false),
                    Chiusura = table.Column<TimeSpan>(type: "time", nullable: false),
                    SaloneId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orario", x => x.OrarioId);
                    table.ForeignKey(
                        name: "FK_Orario_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_Orario_Salone_SaloneId1",
                        column: x => x.SaloneId1,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "Recensione",
                columns: table => new
                {
                    RecensioneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Voto = table.Column<int>(type: "int", nullable: false),
                    Commento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SaloneId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recensione", x => x.RecensioneId);
                    table.ForeignKey(
                        name: "FK_Recensione_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Recensione_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_Recensione_Salone_SaloneId1",
                        column: x => x.SaloneId1,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "SaloneAbbonamento",
                columns: table => new
                {
                    SaloneAbbonamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AbbonamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataInizio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataFine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SaloneId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaloneAbbonamento", x => x.SaloneAbbonamentoId);
                    table.ForeignKey(
                        name: "FK_SaloneAbbonamento_Abbonamento_AbbonamentoId",
                        column: x => x.AbbonamentoId,
                        principalTable: "Abbonamento",
                        principalColumn: "AbbonamentoId");
                    table.ForeignKey(
                        name: "FK_SaloneAbbonamento_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_SaloneAbbonamento_Salone_SaloneId1",
                        column: x => x.SaloneId1,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "SaloneResponsabile",
                columns: table => new
                {
                    SaloneResponsabileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
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
                name: "Servizio",
                columns: table => new
                {
                    ServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Prezzo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Durata = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servizio", x => x.ServizioId);
                    table.ForeignKey(
                        name: "FK_Servizio_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "Slot",
                columns: table => new
                {
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OraInizio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OraFine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Slot", x => x.SlotId);
                    table.ForeignKey(
                        name: "FK_Slot_Salone_SaloneId",
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

            migrationBuilder.CreateTable(
                name: "Appuntamento",
                columns: table => new
                {
                    AppuntamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OraInizio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OraFine = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SlotId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Stato = table.Column<int>(type: "int", nullable: false),
                    SaloneId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SlotId1 = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appuntamento", x => x.AppuntamentoId);
                    table.ForeignKey(
                        name: "FK_Appuntamento_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Appuntamento_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_Appuntamento_Salone_SaloneId1",
                        column: x => x.SaloneId1,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_Appuntamento_Slot_SlotId",
                        column: x => x.SlotId,
                        principalTable: "Slot",
                        principalColumn: "SlotId");
                    table.ForeignKey(
                        name: "FK_Appuntamento_Slot_SlotId1",
                        column: x => x.SlotId1,
                        principalTable: "Slot",
                        principalColumn: "SlotId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_ApplicationUserId",
                table: "Appuntamento",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_SaloneId",
                table: "Appuntamento",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_SaloneId1",
                table: "Appuntamento",
                column: "SaloneId1");

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_SlotId",
                table: "Appuntamento",
                column: "SlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Appuntamento_SlotId1",
                table: "Appuntamento",
                column: "SlotId1");

            migrationBuilder.CreateIndex(
                name: "IX_Immagine_SaloneId",
                table: "Immagine",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Orario_SaloneId",
                table: "Orario",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Orario_SaloneId1",
                table: "Orario",
                column: "SaloneId1");

            migrationBuilder.CreateIndex(
                name: "IX_Recensione_ApplicationUserId",
                table: "Recensione",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensione_SaloneId",
                table: "Recensione",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Recensione_SaloneId1",
                table: "Recensione",
                column: "SaloneId1");

            migrationBuilder.CreateIndex(
                name: "IX_Salone_ApplicationUserId",
                table: "Salone",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneAbbonamento_AbbonamentoId",
                table: "SaloneAbbonamento",
                column: "AbbonamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneAbbonamento_SaloneId",
                table: "SaloneAbbonamento",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_SaloneAbbonamento_SaloneId1",
                table: "SaloneAbbonamento",
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

            migrationBuilder.CreateIndex(
                name: "IX_Servizio_SaloneId",
                table: "Servizio",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Slot_SaloneId",
                table: "Slot",
                column: "SaloneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appuntamento");

            migrationBuilder.DropTable(
                name: "Immagine");

            migrationBuilder.DropTable(
                name: "Orario");

            migrationBuilder.DropTable(
                name: "Recensione");

            migrationBuilder.DropTable(
                name: "SaloneAbbonamento");

            migrationBuilder.DropTable(
                name: "SaloneResponsabile");

            migrationBuilder.DropTable(
                name: "SaloneServizio");

            migrationBuilder.DropTable(
                name: "Slot");

            migrationBuilder.DropTable(
                name: "Abbonamento");

            migrationBuilder.DropTable(
                name: "Servizio");

            migrationBuilder.DropTable(
                name: "Salone");

            migrationBuilder.DropColumn(
                name: "Azienda",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CAP",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Città",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CodiceFiscale",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Cognome",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataRegistrazione",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Indirizzo",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Nome",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PartitaIVA",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Provincia",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RagioneSociale",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SDI",
                table: "AspNetUsers");
        }
    }
}

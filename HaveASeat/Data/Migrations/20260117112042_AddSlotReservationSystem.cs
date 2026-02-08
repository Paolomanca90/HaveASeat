using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlotReservationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Appuntamento",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "SlotReservation",
                columns: table => new
                {
                    SlotReservationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OraInizio = table.Column<TimeSpan>(type: "time", nullable: false),
                    OraFine = table.Column<TimeSpan>(type: "time", nullable: false),
                    DipendenteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ServizioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlotReservation", x => x.SlotReservationId);
                    table.ForeignKey(
                        name: "FK_SlotReservation_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SlotReservation_Dipendente_DipendenteId",
                        column: x => x.DipendenteId,
                        principalTable: "Dipendente",
                        principalColumn: "DipendenteId");
                    table.ForeignKey(
                        name: "FK_SlotReservation_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                    table.ForeignKey(
                        name: "FK_SlotReservation_Servizio_ServizioId",
                        column: x => x.ServizioId,
                        principalTable: "Servizio",
                        principalColumn: "ServizioId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_DipendenteId",
                table: "SlotReservation",
                column: "DipendenteId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_ExpiresAt",
                table: "SlotReservation",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_SaloneDataStatus",
                table: "SlotReservation",
                columns: new[] { "SaloneId", "Data", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_ServizioId",
                table: "SlotReservation",
                column: "ServizioId");

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_SessionStatus",
                table: "SlotReservation",
                columns: new[] { "SessionId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_UniqueActive",
                table: "SlotReservation",
                columns: new[] { "SaloneId", "Data", "OraInizio", "DipendenteId", "Status" },
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SlotReservation_UserId",
                table: "SlotReservation",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SlotReservation");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Appuntamento");
        }
    }
}

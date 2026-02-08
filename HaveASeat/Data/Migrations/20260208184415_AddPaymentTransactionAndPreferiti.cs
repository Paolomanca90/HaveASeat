using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionAndPreferiti : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentTransaction",
                columns: table => new
                {
                    PaymentTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StripeSessionId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    StripeSubscriptionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripeCustomerId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Importo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Valuta = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Stato = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Descrizione = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransaction", x => x.PaymentTransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PaymentTransaction_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateTable(
                name: "Preferito",
                columns: table => new
                {
                    PreferitoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DataAggiunta = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferito", x => x.PreferitoId);
                    table.ForeignKey(
                        name: "FK_Preferito_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Preferito_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_SaloneId",
                table: "PaymentTransaction",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_StripeSessionId",
                table: "PaymentTransaction",
                column: "StripeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransaction_UserId",
                table: "PaymentTransaction",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferito_SaloneId",
                table: "Preferito",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_Preferito_UserSalone",
                table: "Preferito",
                columns: new[] { "ApplicationUserId", "SaloneId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentTransaction");

            migrationBuilder.DropTable(
                name: "Preferito");
        }
    }
}

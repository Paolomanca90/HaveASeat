using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class gift_card : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiftCard",
                columns: table => new
                {
                    GiftCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecipientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RecipientEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SenderEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiftCard", x => x.GiftCardId);
                    table.ForeignKey(
                        name: "FK_GiftCard_AspNetUsers_UsedByUserId",
                        column: x => x.UsedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GiftCard_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_Code",
                table: "GiftCard",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_ExpiryDate",
                table: "GiftCard",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_IsUsed",
                table: "GiftCard",
                column: "IsUsed");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_RecipientEmail",
                table: "GiftCard",
                column: "RecipientEmail");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_SaloneId",
                table: "GiftCard",
                column: "SaloneId");

            migrationBuilder.CreateIndex(
                name: "IX_GiftCard_UsedByUserId",
                table: "GiftCard",
                column: "UsedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiftCard");
        }
    }
}

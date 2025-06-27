using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaveASeat.Data.Migrations
{
    /// <inheritdoc />
    public partial class personalizza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCover",
                table: "Immagine",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "SalonePersonalizzazione",
                columns: table => new
                {
                    SalonePersonalizzazioneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SaloneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TemaColore = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColorePrimario = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    ColoreSecondario = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    ColoreAccento = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LayoutTipo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Slogan = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    InstagramUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FacebookUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TiktokUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MostraGalleria = table.Column<bool>(type: "bit", nullable: false),
                    MostraTeam = table.Column<bool>(type: "bit", nullable: false),
                    MostraServizi = table.Column<bool>(type: "bit", nullable: false),
                    MostraRecensioni = table.Column<bool>(type: "bit", nullable: false),
                    MostraContatti = table.Column<bool>(type: "bit", nullable: false),
                    DataCreazione = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataUltimaModifica = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalonePersonalizzazione", x => x.SalonePersonalizzazioneId);
                    table.ForeignKey(
                        name: "FK_SalonePersonalizzazione_Salone_SaloneId",
                        column: x => x.SaloneId,
                        principalTable: "Salone",
                        principalColumn: "SaloneId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalonePersonalizzazione_SaloneId",
                table: "SalonePersonalizzazione",
                column: "SaloneId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SalonePersonalizzazione");

            migrationBuilder.DropColumn(
                name: "IsCover",
                table: "Immagine");
        }
    }
}

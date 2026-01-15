using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoldbergGUI.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "steamapp",
                columns: table => new
                {
                    appid = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    comparable_name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    type = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    last_modified = table.Column<long>(type: "INTEGER", nullable: false),
                    price_change_number = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_steamapp", x => x.appid);
                });

            migrationBuilder.CreateIndex(
                name: "IX_steamapp_comparable_name",
                table: "steamapp",
                column: "comparable_name");

            migrationBuilder.CreateIndex(
                name: "IX_steamapp_type",
                table: "steamapp",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "steamapp");
        }
    }
}

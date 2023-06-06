using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Donator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "donator",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    expiration_date = table.Column<string>(type: "text", nullable: true),
                    ooc_color = table.Column<string>(type: "text", nullable: false, defaultValue: "#ff0000"),
                    rank = table.Column<string>(type: "text", nullable: false, defaultValue: "Unknown")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donator", x => x.user_id);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "donator");
        }
    }
}

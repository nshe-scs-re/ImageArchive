using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InitializeDatabase.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnixTime = table.Column<long>(type: "bigint", nullable: true),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SiteName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiteNumber = table.Column<int>(type: "int", nullable: true),
                    CameraPositionNumber = table.Column<int>(type: "int", nullable: true),
                    CameraPositionName = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Images");
        }
    }
}

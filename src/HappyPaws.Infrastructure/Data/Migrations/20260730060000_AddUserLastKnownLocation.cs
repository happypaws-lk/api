using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace HappyPaws.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLastKnownLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "LastKnownLocation",
                table: "users",
                type: "geography (point, 4326)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_LastKnownLocation",
                table: "users",
                column: "LastKnownLocation")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_users_LastKnownLocation",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LastKnownLocation",
                table: "users");
        }
    }
}

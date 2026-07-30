using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAwardedByIdFromUserBadge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_badges_users_AwardedById",
                table: "user_badges");

            migrationBuilder.DropIndex(
                name: "IX_user_badges_AwardedById",
                table: "user_badges");

            migrationBuilder.DropColumn(
                name: "AwardedById",
                table: "user_badges");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AwardedById",
                table: "user_badges",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_badges_AwardedById",
                table: "user_badges",
                column: "AwardedById");

            migrationBuilder.AddForeignKey(
                name: "FK_user_badges_users_AwardedById",
                table: "user_badges",
                column: "AwardedById",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}

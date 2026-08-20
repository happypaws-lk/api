using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyPaws.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedCommunityPostFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DropoffContactName",
                table: "transport_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoKey",
                table: "transport_tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PickupContactName",
                table: "transport_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickupTimeEnd",
                table: "transport_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PickupTimeStart",
                table: "transport_tasks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialInstructions",
                table: "transport_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "transport_tasks",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "transport_tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "rescue_cases",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "rescue_cases",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "animal_listings",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "animal_listings",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffContactName",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "PhotoKey",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "PickupContactName",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "PickupTimeEnd",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "PickupTimeStart",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "SpecialInstructions",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "transport_tasks");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "rescue_cases");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "rescue_cases");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "animal_listings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "animal_listings");
        }
    }
}

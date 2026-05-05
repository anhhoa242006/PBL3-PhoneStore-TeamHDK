using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HDKmall.Migrations
{
    /// <inheritdoc />
    public partial class AddRichBannerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BadgeText",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ButtonText",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Banners",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BadgeText", "ButtonText", "Description" },
                values: new object[] { null, null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BadgeText",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ButtonText",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Banners");
        }
    }
}

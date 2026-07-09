using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bookings.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRole : Migration
    {
        // Bootstraps one Admin account so the app is usable without a
        // chicken-and-egg "who promotes the first admin?" problem. The password
        // is a fixed local/demo credential (see README), not a production
        // secret — the hash below is BCrypt(work factor 12) of "AdminPass123!".
        private const string SeedAdminId = "00000000-0000-0000-0000-000000000001";
        private const string SeedAdminPasswordHash = "$2a$12$yWeqNxHGpHfxdMwEzeeI/uMVb25I3FEPabxeccswJ2vXaljN6INNO";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                // Existing rows (and any user created before this migration ran
                // in a fresh environment) default to the least-privileged role.
                defaultValue: "User");

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "Email", "FullName", "PasswordHash", "Role", "CreatedAt" },
                values: new object[]
                {
                    SeedAdminId,
                    "admin@bookings.local",
                    "Admin",
                    SeedAdminPasswordHash,
                    "Admin",
                    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: SeedAdminId);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");
        }
    }
}

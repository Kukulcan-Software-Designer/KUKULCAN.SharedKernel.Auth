using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KUKULCAN.SharedKernel.Auth.Migrations.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class MoveTablesToAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Auth");

            migrationBuilder.RenameTable(
                name: "FederatedIdentities",
                schema: "public",
                newName: "FederatedIdentities",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                schema: "public",
                newName: "TenantMemberships",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "public",
                newName: "Users",
                newSchema: "Auth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "FederatedIdentities",
                schema: "Auth",
                newName: "FederatedIdentities",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                schema: "Auth",
                newName: "TenantMemberships",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "Auth",
                newName: "Users",
                newSchema: "public");

            migrationBuilder.DropSchema(
                name: "Auth");
        }
    }
}

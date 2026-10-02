using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KUKULCAN.SharedKernel.Auth.Migrations.SQLServer.Migrations
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
                schema: "dbo",
                newName: "FederatedIdentities",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                schema: "dbo",
                newName: "TenantMemberships",
                newSchema: "Auth");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "dbo",
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
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "TenantMemberships",
                schema: "Auth",
                newName: "TenantMemberships",
                newSchema: "dbo");

            migrationBuilder.RenameTable(
                name: "Users",
                schema: "Auth",
                newName: "Users",
                newSchema: "dbo");

            migrationBuilder.DropSchema(
                name: "Auth");
        }
    }
}

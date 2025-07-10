using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AwsHelper.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParameterStorages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Path = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParameterStorages", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParameterStorages");
        }
    }
}

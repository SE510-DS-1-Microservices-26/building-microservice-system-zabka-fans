using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCoreForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CandidateLevel",
                table: "InternshipApplications",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "UsersCore",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsersCore", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_InternshipApplications_UsersCore_CandidateId",
                table: "InternshipApplications",
                column: "CandidateId",
                principalTable: "UsersCore",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InternshipApplications_UsersCore_CandidateId",
                table: "InternshipApplications");

            migrationBuilder.DropTable(
                name: "UsersCore");

            migrationBuilder.AlterColumn<int>(
                name: "CandidateLevel",
                table: "InternshipApplications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}

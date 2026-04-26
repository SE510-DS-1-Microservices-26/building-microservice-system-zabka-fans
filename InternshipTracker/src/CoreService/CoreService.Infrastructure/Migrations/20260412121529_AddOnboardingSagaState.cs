using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingSagaState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OnboardingSagaState",
                columns: table => new
                {
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentState = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    CandidateName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CandidateEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    CorporateEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    FaultReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingSagaState", x => x.CorrelationId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OnboardingSagaState");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loopai.CloudApi.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationResults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ValidationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsValid = table.Column<bool>(type: "bit", nullable: false),
                    ValidationScore = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    Errors = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidationMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValidatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ValidationResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_ExecutionId",
                table: "ValidationResults",
                column: "ExecutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_ProgramId_IsValid_ValidatedAt",
                table: "ValidationResults",
                columns: new[] { "ProgramId", "IsValid", "ValidatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_ProgramId_ValidatedAt",
                table: "ValidationResults",
                columns: new[] { "ProgramId", "ValidatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ValidationResults_TaskId_ValidatedAt",
                table: "ValidationResults",
                columns: new[] { "TaskId", "ValidatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ValidationResults");
        }
    }
}

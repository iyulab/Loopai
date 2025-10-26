using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Loopai.CloudApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExecutionRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InputData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LatencyMs = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    MemoryUsageMb = table.Column<double>(type: "float(10)", precision: 10, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SampledForValidation = table.Column<bool>(type: "bit", nullable: false),
                    ValidationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProgramArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Language = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SynthesisStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    ComplexityMetrics_CyclomaticComplexity = table.Column<int>(type: "int", nullable: true),
                    ComplexityMetrics_LinesOfCode = table.Column<int>(type: "int", nullable: true),
                    ComplexityMetrics_EstimatedLatencyMs = table.Column<double>(type: "float", nullable: true),
                    LlmProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LlmModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GenerationCost = table.Column<double>(type: "float", nullable: false),
                    GenerationTimeSec = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DeploymentPercentage = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramArtifacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InputSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AccuracyTarget = table.Column<double>(type: "float(3)", precision: 3, scale: 2, nullable: false),
                    LatencyTargetMs = table.Column<int>(type: "int", nullable: false),
                    SamplingRate = table.Column<double>(type: "float(3)", precision: 3, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRecords_ProgramId_ExecutedAt",
                table: "ExecutionRecords",
                columns: new[] { "ProgramId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRecords_TaskId_ExecutedAt",
                table: "ExecutionRecords",
                columns: new[] { "TaskId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionRecords_TaskId_SampledForValidation_ExecutedAt",
                table: "ExecutionRecords",
                columns: new[] { "TaskId", "SampledForValidation", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramArtifacts_TaskId_Status",
                table: "ProgramArtifacts",
                columns: new[] { "TaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ProgramArtifacts_TaskId_Version",
                table: "ProgramArtifacts",
                columns: new[] { "TaskId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Name",
                table: "Tasks",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExecutionRecords");

            migrationBuilder.DropTable(
                name: "ProgramArtifacts");

            migrationBuilder.DropTable(
                name: "Tasks");
        }
    }
}

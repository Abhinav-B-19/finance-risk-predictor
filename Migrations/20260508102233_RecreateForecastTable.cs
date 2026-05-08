using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinanceRiskAPI.Migrations
{
    public partial class RecreateForecastTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Forecasts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),

                    PredictionId = table.Column<int>(
                        type: "integer",
                        nullable: false),

                    ForecastMonth = table.Column<string>(
                        type: "text",
                        nullable: false),

                    RiskScore = table.Column<double>(
                        type: "double precision",
                        nullable: false),

                    RiskLevel = table.Column<string>(
                        type: "text",
                        nullable: false),

                    GeneratedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey(
                        "PK_Forecasts",
                        x => x.Id);

                    table.ForeignKey(
                        name: "FK_Forecasts_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Forecasts_PredictionId",
                table: "Forecasts",
                column: "PredictionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Forecasts");
        }
    }
}
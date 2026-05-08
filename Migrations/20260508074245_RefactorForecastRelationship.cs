using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceRiskAPI.Migrations
{
    /// <inheritdoc />
    public partial class RefactorForecastRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forecasts_Users_UserId",
                table: "Forecasts");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Forecasts",
                newName: "PredictionId");

            migrationBuilder.RenameIndex(
                name: "IX_Forecasts_UserId",
                table: "Forecasts",
                newName: "IX_Forecasts_PredictionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Forecasts_Predictions_PredictionId",
                table: "Forecasts",
                column: "PredictionId",
                principalTable: "Predictions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Forecasts_Predictions_PredictionId",
                table: "Forecasts");

            migrationBuilder.RenameColumn(
                name: "PredictionId",
                table: "Forecasts",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Forecasts_PredictionId",
                table: "Forecasts",
                newName: "IX_Forecasts_UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Forecasts_Users_UserId",
                table: "Forecasts",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}

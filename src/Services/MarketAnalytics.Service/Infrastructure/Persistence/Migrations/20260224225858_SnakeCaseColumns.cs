using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_aggregated_market_analytics",
                table: "aggregated_market_analytics");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "aggregated_market_analytics",
                newName: "volume");

            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "aggregated_market_analytics",
                newName: "symbol");

            migrationBuilder.RenameColumn(
                name: "WindowEndUtc",
                table: "aggregated_market_analytics",
                newName: "window_end_utc");

            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "aggregated_market_analytics",
                newName: "updated_at_utc");

            migrationBuilder.RenameColumn(
                name: "TradesCount",
                table: "aggregated_market_analytics",
                newName: "trades_count");

            migrationBuilder.RenameColumn(
                name: "OpenPrice",
                table: "aggregated_market_analytics",
                newName: "open_price");

            migrationBuilder.RenameColumn(
                name: "LowPrice",
                table: "aggregated_market_analytics",
                newName: "low_price");

            migrationBuilder.RenameColumn(
                name: "LastPrice",
                table: "aggregated_market_analytics",
                newName: "last_price");

            migrationBuilder.RenameColumn(
                name: "HighPrice",
                table: "aggregated_market_analytics",
                newName: "high_price");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "aggregated_market_analytics",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "WindowStartUtc",
                table: "aggregated_market_analytics",
                newName: "window_start_utc");

            migrationBuilder.AddPrimaryKey(
                name: "pk_aggregated_market_analytics",
                table: "aggregated_market_analytics",
                columns: new[] { "symbol", "window_start_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_aggregated_market_analytics",
                table: "aggregated_market_analytics");

            migrationBuilder.RenameColumn(
                name: "volume",
                table: "aggregated_market_analytics",
                newName: "Volume");

            migrationBuilder.RenameColumn(
                name: "symbol",
                table: "aggregated_market_analytics",
                newName: "Symbol");

            migrationBuilder.RenameColumn(
                name: "window_end_utc",
                table: "aggregated_market_analytics",
                newName: "WindowEndUtc");

            migrationBuilder.RenameColumn(
                name: "updated_at_utc",
                table: "aggregated_market_analytics",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "trades_count",
                table: "aggregated_market_analytics",
                newName: "TradesCount");

            migrationBuilder.RenameColumn(
                name: "open_price",
                table: "aggregated_market_analytics",
                newName: "OpenPrice");

            migrationBuilder.RenameColumn(
                name: "low_price",
                table: "aggregated_market_analytics",
                newName: "LowPrice");

            migrationBuilder.RenameColumn(
                name: "last_price",
                table: "aggregated_market_analytics",
                newName: "LastPrice");

            migrationBuilder.RenameColumn(
                name: "high_price",
                table: "aggregated_market_analytics",
                newName: "HighPrice");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "aggregated_market_analytics",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "window_start_utc",
                table: "aggregated_market_analytics",
                newName: "WindowStartUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_aggregated_market_analytics",
                table: "aggregated_market_analytics",
                columns: new[] { "Symbol", "WindowStartUtc" });
        }
    }
}

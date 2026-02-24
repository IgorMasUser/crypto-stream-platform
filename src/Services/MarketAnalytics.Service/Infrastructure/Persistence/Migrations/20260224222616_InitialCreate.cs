using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingApp.MarketAnalytics.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "aggregated_market_analytics",
                columns: table => new
                {
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WindowStartUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEndUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    HighPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    LowPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    LastPrice = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    TradesCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_aggregated_market_analytics", x => new { x.Symbol, x.WindowStartUtc });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "aggregated_market_analytics");
        }
    }
}

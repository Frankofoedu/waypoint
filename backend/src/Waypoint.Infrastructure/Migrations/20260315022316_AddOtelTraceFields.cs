using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Waypoint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtelTraceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "TraceFlags",
                table: "Traces",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceState",
                table: "Traces",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TraceFlags",
                table: "Traces");

            migrationBuilder.DropColumn(
                name: "TraceState",
                table: "Traces");
        }
    }
}

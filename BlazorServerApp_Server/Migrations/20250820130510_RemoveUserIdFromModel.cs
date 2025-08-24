using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorServerApp_Server.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserIdFromModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AdvancedNavigationLogEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "AdvancedNavigationLogEntries",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

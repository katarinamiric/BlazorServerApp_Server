using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlazorServerApp_Server.Migrations
{
    /// <inheritdoc />
    public partial class AddedAdvancedNavigationLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvancedNavigationLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PreviousPage1Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousPage2Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PreviousPage3Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeOfDayInHours = table.Column<float>(type: "real", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NextPageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvancedNavigationLogEntries", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvancedNavigationLogEntries");
        }
    }
}

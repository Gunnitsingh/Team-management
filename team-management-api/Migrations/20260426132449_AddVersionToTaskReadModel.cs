using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace team_management_api.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionToTaskReadModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "TaskReadModels",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Version",
                table: "TaskReadModels");
        }
    }
}

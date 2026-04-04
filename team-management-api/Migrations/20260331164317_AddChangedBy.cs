using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace team_management_api.Migrations
{
    /// <inheritdoc />
    public partial class AddChangedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChangedBy",
                table: "TaskActivities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ChangedByName",
                table: "TaskActivities",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangedBy",
                table: "TaskActivities");

            migrationBuilder.DropColumn(
                name: "ChangedByName",
                table: "TaskActivities");
        }
    }
}

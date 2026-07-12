using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CVManager.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionProjectTagsAndFixProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProjectTags",
                table: "Positions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProjectTags",
                table: "Positions");
        }
    }
}

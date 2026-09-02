using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectService.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxVersionDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "OutboxMessages",
                type: "integer",
                rowVersion: true,
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "integer",
                oldRowVersion: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Version",
                table: "OutboxMessages",
                type: "integer",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldRowVersion: true,
                oldDefaultValue: 1);
        }
    }
}

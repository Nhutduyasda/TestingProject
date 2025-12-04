using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Migrations
{
    /// <inheritdoc />
    public partial class AddComboToCartAndInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "CartDetails",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ComboId",
                table: "CartDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartDetails_ComboId",
                table: "CartDetails",
                column: "ComboId");

            migrationBuilder.AddForeignKey(
                name: "FK_CartDetails_Combos_ComboId",
                table: "CartDetails",
                column: "ComboId",
                principalTable: "Combos",
                principalColumn: "ComboId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartDetails_Combos_ComboId",
                table: "CartDetails");

            migrationBuilder.DropIndex(
                name: "IX_CartDetails_ComboId",
                table: "CartDetails");

            migrationBuilder.DropColumn(
                name: "ComboId",
                table: "CartDetails");

            migrationBuilder.AlterColumn<int>(
                name: "VariantId",
                table: "CartDetails",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}

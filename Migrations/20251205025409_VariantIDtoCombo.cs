using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyProject.Migrations
{
    /// <inheritdoc />
    public partial class VariantIDtoCombo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "ComboProducts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComboProducts_VariantId",
                table: "ComboProducts",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComboProducts_Variants_VariantId",
                table: "ComboProducts",
                column: "VariantId",
                principalTable: "Variants",
                principalColumn: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComboProducts_Variants_VariantId",
                table: "ComboProducts");

            migrationBuilder.DropIndex(
                name: "IX_ComboProducts_VariantId",
                table: "ComboProducts");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "ComboProducts");
        }
    }
}

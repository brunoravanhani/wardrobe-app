using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1707

namespace VirtualWardrobe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class _20260611_AddWishlistLinkLabel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "label",
                table: "wishlist_external_links",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "label",
                table: "wishlist_external_links");
        }
    }
}

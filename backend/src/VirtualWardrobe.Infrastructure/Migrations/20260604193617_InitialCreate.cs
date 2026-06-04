using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualWardrobe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        private static readonly string[] UserIdCreatedAtUtcColumns = ["user_id", "created_at_utc"];
        private static readonly string[] UserIdCategoryColumns = ["user_id", "category"];
        private static readonly string[] WishlistItemIdUrlColumns = ["wishlist_item_id", "url"];
        private static readonly string[] UserIdStatusColumns = ["user_id", "status"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    google_subject = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "media_assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_size_bytes = table.Column<int>(type: "integer", nullable: false),
                    visibility = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "FK_media_assets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wardrobe_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    size = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    body_image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    care_tag_image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wardrobe_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_wardrobe_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    brand = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    target_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    inspiration_image_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    purchased_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    converted_wardrobe_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_wishlist_items_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "wishlist_external_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wishlist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wishlist_external_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_wishlist_external_links_wishlist_items_wishlist_item_id",
                        column: x => x.wishlist_item_id,
                        principalTable: "wishlist_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_storage_key",
                table: "media_assets",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_media_assets_user_id_created_at_utc",
                table: "media_assets",
                columns: UserIdCreatedAtUtcColumns);

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "IX_users_google_subject",
                table: "users",
                column: "google_subject",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wardrobe_items_user_id_category",
                table: "wardrobe_items",
                columns: UserIdCategoryColumns);

            migrationBuilder.CreateIndex(
                name: "IX_wardrobe_items_user_id_created_at_utc",
                table: "wardrobe_items",
                columns: UserIdCreatedAtUtcColumns);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_external_links_wishlist_item_id",
                table: "wishlist_external_links",
                column: "wishlist_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_external_links_wishlist_item_id_url",
                table: "wishlist_external_links",
                columns: WishlistItemIdUrlColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_user_id_created_at_utc",
                table: "wishlist_items",
                columns: UserIdCreatedAtUtcColumns);

            migrationBuilder.CreateIndex(
                name: "IX_wishlist_items_user_id_status",
                table: "wishlist_items",
                columns: UserIdStatusColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets");

            migrationBuilder.DropTable(
                name: "wardrobe_items");

            migrationBuilder.DropTable(
                name: "wishlist_external_links");

            migrationBuilder.DropTable(
                name: "wishlist_items");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}

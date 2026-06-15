using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VirtualWardrobe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWardrobeTemplates : Migration
    {
        private static readonly string[] _userIdCategoryColumns = ["user_id", "category"];
        private static readonly string[] _userIdTemplateIdColumns = ["user_id", "template_id"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "active_template_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "wardrobe_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wardrobe_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "template_slot_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_slot_definitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_slot_definitions_wardrobe_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "wardrobe_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_slots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    wardrobe_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    wishlist_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fulfilled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_slots", x => x.id);
                    table.ForeignKey(
                        name: "FK_template_slots_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_template_slots_wardrobe_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "wardrobe_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_active_template_id",
                table: "users",
                column: "active_template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_slot_definitions_template_id",
                table: "template_slot_definitions",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_slots_template_id",
                table: "template_slots",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "IX_template_slots_user_id_category",
                table: "template_slots",
                columns: _userIdCategoryColumns);

            migrationBuilder.CreateIndex(
                name: "IX_template_slots_user_id_template_id",
                table: "template_slots",
                columns: _userIdTemplateIdColumns);

            migrationBuilder.CreateIndex(
                name: "IX_template_slots_wardrobe_item_id",
                table: "template_slots",
                column: "wardrobe_item_id",
                unique: true,
                filter: "wardrobe_item_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_users_wardrobe_templates_active_template_id",
                table: "users",
                column: "active_template_id",
                principalTable: "wardrobe_templates",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_wardrobe_templates_active_template_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "template_slot_definitions");

            migrationBuilder.DropTable(
                name: "template_slots");

            migrationBuilder.DropTable(
                name: "wardrobe_templates");

            migrationBuilder.DropIndex(
                name: "IX_users_active_template_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "active_template_id",
                table: "users");
        }
    }
}

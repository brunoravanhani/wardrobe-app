using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1707

namespace VirtualWardrobe.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultTemplates : Migration
    {
        private static readonly Guid CapsuleId = new("a1000000-0000-0000-0000-000000000001");
        private static readonly Guid TrabalhoId = new("a1000000-0000-0000-0000-000000000002");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "wardrobe_templates",
                columns: ["id", "name"],
                values: new object[] { CapsuleId, "Capsula" });

            migrationBuilder.InsertData(
                table: "wardrobe_templates",
                columns: ["id", "name"],
                values: new object[] { TrabalhoId, "Trabalho" });

            // Capsula: 8 TShirt, 3 Shirt, 3 Pants, 3 Shorts, 3 Shoes  (= 20 total)
            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000001"), CapsuleId, "TShirt", 8 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000002"), CapsuleId, "Shirt", 3 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000003"), CapsuleId, "Pants", 3 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000004"), CapsuleId, "Shorts", 3 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000005"), CapsuleId, "Shoes", 3 });

            // Trabalho: 5 Shirt, 3 Trousers, 1 Shoes  (= 9 total)
            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000006"), TrabalhoId, "Shirt", 5 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000007"), TrabalhoId, "Trousers", 3 });

            migrationBuilder.InsertData(
                table: "template_slot_definitions",
                columns: ["id", "template_id", "category", "quantity"],
                values: new object[] { new Guid("b1000000-0000-0000-0000-000000000008"), TrabalhoId, "Shoes", 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "wardrobe_templates", keyColumn: "id", keyValue: CapsuleId);
            migrationBuilder.DeleteData(table: "wardrobe_templates", keyColumn: "id", keyValue: TrabalhoId);
        }
    }
}

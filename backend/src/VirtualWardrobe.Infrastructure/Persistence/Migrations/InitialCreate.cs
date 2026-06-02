using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace VirtualWardrobe.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
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

        migrationBuilder.CreateIndex(
            name: "IX_media_assets_storage_key",
            table: "media_assets",
            column: "storage_key",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_media_assets_user_id_created_at_utc",
            table: "media_assets",
            columns: new[] { "user_id", "created_at_utc" });

        migrationBuilder.CreateIndex(
            name: "IX_users_email",
            table: "users",
            column: "email");

        migrationBuilder.CreateIndex(
            name: "IX_users_google_subject",
            table: "users",
            column: "google_subject",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "media_assets");

        migrationBuilder.DropTable(
            name: "users");
    }
}
#pragma warning restore CA1861
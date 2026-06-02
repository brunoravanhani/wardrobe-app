using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using VirtualWardrobe.Infrastructure.Persistence;

#nullable disable

namespace VirtualWardrobe.Infrastructure.Persistence.Migrations;

[DbContext(typeof(VirtualWardrobeDbContext))]
sealed partial class VirtualWardrobeDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity("VirtualWardrobe.Infrastructure.Persistence.Entities.MediaAssetRecord", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<string>("ContentType")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("content_type");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at_utc");

                b.Property<int>("FileSizeBytes")
                    .HasColumnType("integer")
                    .HasColumnName("file_size_bytes");

                b.Property<string>("StorageKey")
                    .IsRequired()
                    .HasMaxLength(512)
                    .HasColumnType("character varying(512)")
                    .HasColumnName("storage_key");

                b.Property<Guid>("UserId")
                    .HasColumnType("uuid")
                    .HasColumnName("user_id");

                b.Property<string>("Visibility")
                    .IsRequired()
                    .HasMaxLength(64)
                    .HasColumnType("character varying(64)")
                    .HasColumnName("visibility");

                b.HasKey("Id")
                    .HasName("PK_media_assets");

                b.HasIndex("StorageKey")
                    .IsUnique()
                    .HasDatabaseName("IX_media_assets_storage_key");

                b.HasIndex("UserId", "CreatedAtUtc")
                    .HasDatabaseName("IX_media_assets_user_id_created_at_utc");

                b.ToTable("media_assets", (string)null);
            });

        modelBuilder.Entity("VirtualWardrobe.Infrastructure.Persistence.Entities.UserRecord", b =>
            {
                b.Property<Guid>("Id")
                    .HasColumnType("uuid")
                    .HasColumnName("id");

                b.Property<DateTime>("CreatedAtUtc")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("created_at_utc");

                b.Property<string>("DisplayName")
                    .HasMaxLength(160)
                    .HasColumnType("character varying(160)")
                    .HasColumnName("display_name");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasMaxLength(256)
                    .HasColumnType("character varying(256)")
                    .HasColumnName("email");

                b.Property<string>("GoogleSubject")
                    .IsRequired()
                    .HasMaxLength(128)
                    .HasColumnType("character varying(128)")
                    .HasColumnName("google_subject");

                b.Property<string>("Locale")
                    .IsRequired()
                    .HasMaxLength(16)
                    .HasColumnType("character varying(16)")
                    .HasColumnName("locale");

                b.Property<DateTime>("UpdatedAtUtc")
                    .HasColumnType("timestamp with time zone")
                    .HasColumnName("updated_at_utc");

                b.HasKey("Id")
                    .HasName("PK_users");

                b.HasIndex("Email")
                    .HasDatabaseName("IX_users_email");

                b.HasIndex("GoogleSubject")
                    .IsUnique()
                    .HasDatabaseName("IX_users_google_subject");

                b.ToTable("users", (string)null);
            });

        modelBuilder.Entity("VirtualWardrobe.Infrastructure.Persistence.Entities.MediaAssetRecord", b =>
            {
                b.HasOne("VirtualWardrobe.Infrastructure.Persistence.Entities.UserRecord", "User")
                    .WithMany("MediaAssets")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("User");
            });

        modelBuilder.Entity("VirtualWardrobe.Infrastructure.Persistence.Entities.UserRecord", b =>
            {
                b.Navigation("MediaAssets");
            });
#pragma warning restore 612, 618
    }
}
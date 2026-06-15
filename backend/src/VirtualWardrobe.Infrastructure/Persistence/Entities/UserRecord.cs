namespace VirtualWardrobe.Infrastructure.Persistence.Entities;

public sealed class UserRecord
{
    public Guid Id { get; set; }

    public string GoogleSubject { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string Locale { get; set; } = "pt-BR";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Guid? ActiveTemplateId { get; set; }

    public WardrobeTemplateRecord? ActiveTemplate { get; set; }

    public List<MediaAssetRecord> MediaAssets { get; set; } = [];
}
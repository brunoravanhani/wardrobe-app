using VirtualWardrobe.Domain.Templates;

namespace VirtualWardrobe.Application.Templates;

public sealed class GetTemplatesQuery
{
    private readonly IWardrobeTemplateRepository _templateRepository;

    public GetTemplatesQuery(IWardrobeTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public Task<IReadOnlyList<WardrobeTemplate>> ExecuteAsync(CancellationToken cancellationToken)
    {
        return _templateRepository.GetAllAsync(cancellationToken);
    }
}

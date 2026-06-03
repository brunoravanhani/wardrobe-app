using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Storage;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IPrivateMediaUrlService _privateMediaUrlService;

    public MediaController(IPrivateMediaUrlService privateMediaUrlService)
    {
        _privateMediaUrlService = privateMediaUrlService;
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<CreateUploadUrlResponse>> CreateUploadUrlAsync(
        [FromBody] CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        var result = await CreateUploadResultAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid media upload request",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var value = result.Value;
        return Ok(new CreateUploadUrlResponse(
            value.MediaAssetId,
            value.UploadUrl,
            value.ExpiresAtUtc,
            value.RequiredHeaders));
    }

    [HttpPost("{mediaAssetId:guid}/view-url")]
    public async Task<ActionResult<CreateViewUrlResponse>> CreateViewUrlAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();

        var viewResult = await _privateMediaUrlService.CreateViewUrlAsync(mediaAssetId, ownerUserId, cancellationToken);
        return Ok(new CreateViewUrlResponse(viewResult.ViewUrl, viewResult.ExpiresAtUtc));
    }

    private async Task<Result<PresignedUploadResult>> CreateUploadResultAsync(
        CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var ownerUserId = User.GetRequiredUserId();
            var uploadResult = await _privateMediaUrlService.CreateUploadUrlAsync(
                new PresignedUploadRequest(
                    request.FileName,
                    request.ContentType,
                    request.FileSizeBytes,
                    request.Purpose,
                    ownerUserId),
                cancellationToken);

            return Result.Success(uploadResult);
        }
        catch (ArgumentException exception)
        {
            return Result.Failure<PresignedUploadResult>(ResultError.Validation(exception.Message));
        }
    }
}

public sealed record CreateUploadUrlRequest(
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Purpose);

public sealed record CreateUploadUrlResponse(
    Guid MediaAssetId,
    Uri UploadUrl,
    DateTime ExpiresAtUtc,
    IReadOnlyDictionary<string, string> RequiredHeaders);

public sealed record CreateViewUrlResponse(Uri Url, DateTime ExpiresAtUtc);

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualWardrobe.Api.Infrastructure;
using VirtualWardrobe.Api.Observability;
using VirtualWardrobe.Application.Common;
using VirtualWardrobe.Application.Storage;

namespace VirtualWardrobe.Api.Controllers;

[ApiController]
[Authorize]
[Route("v1/media")]
public sealed partial class MediaController : ControllerBase
{
    private readonly IPrivateMediaUrlService _privateMediaUrlService;
    private readonly ILogger<MediaController> _logger;

    public MediaController(IPrivateMediaUrlService privateMediaUrlService, ILogger<MediaController> logger)
    {
        _privateMediaUrlService = privateMediaUrlService;
        _logger = logger;
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<CreateUploadUrlResponse>> CreateUploadUrlAsync(
        [FromBody] CreateUploadUrlRequest request,
        CancellationToken cancellationToken)
    {
        TelemetryConfig.MediaUploadUrlTotal.Add(1);
        Log.UploadUrlRequested(_logger, request.Purpose);

        var result = await CreateUploadResultAsync(request, cancellationToken);
        if (result.IsFailure)
        {
            TelemetryConfig.MediaPresignFailures.Add(1);
            Log.UploadUrlFailed(_logger, result.Error.Message);
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid media upload request",
                Detail = result.Error.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var value = result.Value;
        Log.UploadUrlIssued(_logger, value.MediaAssetId);
        return Ok(new CreateUploadUrlResponse(
            value.MediaAssetId,
            value.UploadUrl,
            value.ExpiresAtUtc,
            value.RequiredHeaders));
    }

    [HttpPost("{mediaAssetId:guid}/view-url")]
    public async Task<ActionResult<CreateViewUrlResponse>> CreateViewUrlAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        TelemetryConfig.MediaViewUrlTotal.Add(1);
        var ownerUserId = User.GetRequiredUserId();

        Log.ViewUrlRequested(_logger, mediaAssetId, ownerUserId);
        try
        {
            var viewResult = await _privateMediaUrlService.CreateViewUrlAsync(mediaAssetId, ownerUserId, cancellationToken);
            return Ok(new CreateViewUrlResponse(viewResult.ViewUrl, viewResult.ExpiresAtUtc));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{mediaAssetId:guid}")]
    public async Task<IActionResult> DeleteMediaAssetAsync(Guid mediaAssetId, CancellationToken cancellationToken)
    {
        var ownerUserId = User.GetRequiredUserId();
        Log.DeleteRequested(_logger, mediaAssetId, ownerUserId);
        await _privateMediaUrlService.DeleteMediaAssetAsync(mediaAssetId, ownerUserId, cancellationToken);
        return NoContent();
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

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Presigned upload URL requested for purpose {Purpose}")]
        internal static partial void UploadUrlRequested(ILogger logger, string purpose);

        [LoggerMessage(Level = LogLevel.Warning, Message = "Presigned upload URL generation failed: {Error}")]
        internal static partial void UploadUrlFailed(ILogger logger, string error);

        [LoggerMessage(Level = LogLevel.Information, Message = "Presigned upload URL issued for asset {MediaAssetId}")]
        internal static partial void UploadUrlIssued(ILogger logger, Guid mediaAssetId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Presigned view URL requested for asset {MediaAssetId} by user {UserId}")]
        internal static partial void ViewUrlRequested(ILogger logger, Guid mediaAssetId, Guid userId);

        [LoggerMessage(Level = LogLevel.Information, Message = "Delete requested for asset {MediaAssetId} by user {UserId}")]
        internal static partial void DeleteRequested(ILogger logger, Guid mediaAssetId, Guid userId);
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

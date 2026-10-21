using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.UseCases.Reports;
using Microsoft.AspNetCore.Mvc;

namespace FoodSocial.Post.Api.Controllers;

/// <summary>
/// Exposes the Post moderation report endpoints (submit + admin review/resolve).
/// </summary>
[ApiController]
public sealed class PostReportsController(PostReportUseCase reportUseCase, PostApiMetrics metrics) : ControllerBase
{
    private readonly PostReportUseCase _reportUseCase = reportUseCase ?? throw new ArgumentNullException(nameof(reportUseCase));
    private readonly PostApiMetrics _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));

    [HttpPost("api/posts/{postId:guid}/report")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    public Task<ActionResult<ContentReportResponse>> ReportPost(
        Guid postId,
        [FromBody] CreateContentReportRequest request,
        CancellationToken cancellationToken) =>
        ReportInternalAsync("Post", () =>
        {
            var securedRequest = request with { ReporterId = PostInternalAuthorization.GetIdentityId(HttpContext) };
            return _reportUseCase.ReportPostAsync(postId, securedRequest, cancellationToken);
        });

    [HttpPost("api/comments/{commentId:guid}/report")]
    [ServiceFilter(typeof(PostInternalAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status201Created)]
    public Task<ActionResult<ContentReportResponse>> ReportComment(
        Guid commentId,
        [FromBody] CreateContentReportRequest request,
        CancellationToken cancellationToken) =>
        ReportInternalAsync("Comment", () =>
        {
            var securedRequest = request with { ReporterId = PostInternalAuthorization.GetIdentityId(HttpContext) };
            return _reportUseCase.ReportCommentAsync(commentId, securedRequest, cancellationToken);
        });

    [HttpGet("api/admin/reports")]
    [ServiceFilter(typeof(PostModeratorAuthorizationFilter))]
    [ProducesResponseType<AdminReportsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminReportsResponse>> GetReports([FromQuery] int? limit, CancellationToken cancellationToken) =>
        Ok(await _reportUseCase.GetReportsAsync(limit, cancellationToken));

    [HttpPost("api/admin/reports/{reportId:guid}/resolve")]
    [ServiceFilter(typeof(PostModeratorAuthorizationFilter))]
    [ProducesResponseType<ContentReportResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ContentReportResponse>> ResolveReport(
        Guid reportId,
        [FromBody] ResolveContentReportRequest request,
        CancellationToken cancellationToken)
    {
        var securedRequest = request with { ModeratorId = PostInternalAuthorization.GetIdentityId(HttpContext) };
        var result = await _reportUseCase.ResolveReportAsync(reportId, securedRequest, cancellationToken);
        if (result.Status == PostOperationStatus.Resolved)
        {
            _metrics.RecordContentReportResolved(securedRequest.Action.Trim().ToLowerInvariant());
        }

        return result.Status switch
        {
            PostOperationStatus.Resolved => Ok(result.Report),
            PostOperationStatus.NotFound => Problem(title: "Report not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid report resolution request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }

    private async Task<ActionResult<ContentReportResponse>> ReportInternalAsync(string targetType, Func<Task<ReportMutationResult>> action)
    {
        var result = await action();
        if (result.Status == PostOperationStatus.Created)
        {
            _metrics.RecordContentReportCreated(targetType);
        }

        return result.Status switch
        {
            PostOperationStatus.Created => Created($"/api/admin/reports/{result.Report!.Id}", result.Report),
            PostOperationStatus.NotFound => Problem(title: "Report target not found", detail: result.Error, statusCode: StatusCodes.Status404NotFound),
            _ => Problem(title: "Invalid report request", detail: result.Error, statusCode: StatusCodes.Status400BadRequest)
        };
    }
}

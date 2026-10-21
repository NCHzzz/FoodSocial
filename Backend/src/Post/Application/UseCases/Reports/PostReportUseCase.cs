using FoodSocial.Contracts.Posts;
using FoodSocial.Post.Application.Mapping;
using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Application.UseCases.Reports;

public sealed class PostReportUseCase(IPostStore postStore, PostResponseMapper mapper)
{
    private readonly IPostStore _postStore = postStore ?? throw new ArgumentNullException(nameof(postStore));
    private readonly PostResponseMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

    public async Task<ReportMutationResult> ReportPostAsync(Guid postId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await CreateReportAsync(ReportTargetType.Post, postId, request, cancellationToken);

public async Task<ReportMutationResult> ReportCommentAsync(Guid commentId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await CreateReportAsync(ReportTargetType.Comment, commentId, request, cancellationToken);

    public async Task<ReportMutationResult> ReportUserAsync(Guid userId, CreateContentReportRequest request, CancellationToken cancellationToken) =>
        await CreateReportAsync(ReportTargetType.User, userId, request, cancellationToken);

    public async Task<AdminReportsResponse> GetReportsAsync(int? limit, CancellationToken cancellationToken)
    {
        var boundedLimit = Math.Clamp(limit ?? PostSearchCriteria.DefaultLimit, 1, PostSearchCriteria.MaxLimit);
        var reports = await _postStore.GetReportsAsync(boundedLimit, cancellationToken);
        return new AdminReportsResponse(reports.Select(_mapper.MapReport).ToArray(), boundedLimit);
    }

    public async Task<ReportMutationResult> ResolveReportAsync(Guid reportId, ResolveContentReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (reportId == Guid.Empty || request.ModeratorId == Guid.Empty || string.IsNullOrWhiteSpace(request.Action) || string.IsNullOrWhiteSpace(request.AuditNote)) return new ReportMutationResult(PostOperationStatus.Invalid, null, "Report id, moderator id, action, and audit note are required.");
        var report = await _postStore.ResolveReportAsync(reportId, request.ModeratorId, request.Action.Trim(), request.AuditNote.Trim(), cancellationToken);
        return report is null ? new ReportMutationResult(PostOperationStatus.NotFound, null, "The requested report was not found.") : new ReportMutationResult(PostOperationStatus.Resolved, _mapper.MapReport(report), null);
    }

    private async Task<ReportMutationResult> CreateReportAsync(ReportTargetType targetType, Guid targetId, CreateContentReportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (targetId == Guid.Empty || request.ReporterId == Guid.Empty || string.IsNullOrWhiteSpace(request.Reason)) return new ReportMutationResult(PostOperationStatus.Invalid, null, "Target id, reporter id, and reason are required.");
        var report = await _postStore.CreateReportAsync(targetType, targetId, request.ReporterId, request.Reason.Trim(), request.Description, cancellationToken);
        return report is null ? new ReportMutationResult(PostOperationStatus.NotFound, null, "The requested report target was not found.") : new ReportMutationResult(PostOperationStatus.Created, _mapper.MapReport(report), null);
    }
}

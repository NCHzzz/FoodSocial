using System.Globalization;
using FoodSocial.Contracts.Posts;
using FoodSocial.Contracts.UserProfiles;

namespace FoodSocial.Bff;

/// <summary>
/// Deep module that composes the Following feed surface from two downstream
/// services: the follow graph (UserProfile) and the post feed (Post).
/// </summary>
/// <remarks>
/// The composer is the BFF-side realization of the "Following" product surface
/// defined in <c>docs/project-discovery/04_functional_requirements.md</c>. It
/// owns:
/// <list type="bullet">
///   <item>limit clamping (1..100, default 20)</item>
///   <item>the empty-following short-circuit (returns empty list without calling Post)</item>
///   <item>per-upstream 502 fallback rules (downstream failures degrade to empty section, not 5xx)</item>
///   <item>the group-by-Id + sort-by-Date + take(limit) composition logic</item>
/// </list>
/// Tests target this interface directly: a "Following feed returns empty when
/// user follows nobody" test exercises the composer's surface, not a BFF
/// endpoint harness.
/// </remarks>
internal sealed class FollowingFeedComposer(
    UserProfileHttpClient userProfileClient,
    PostHttpClient postClient,
    ILogger<FollowingFeedComposer> logger)
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    private readonly UserProfileHttpClient _userProfileClient = userProfileClient ?? throw new ArgumentNullException(nameof(userProfileClient));
    private readonly PostHttpClient _postClient = postClient ?? throw new ArgumentNullException(nameof(postClient));
    private readonly ILogger<FollowingFeedComposer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Composes the Following feed for <paramref name="identityId"/>.
    /// </summary>
    /// <param name="identityId">The authenticated identity whose followed authors drive the feed.</param>
    /// <param name="requestedLimit">Optional limit (1..100). When null or out-of-range, <see cref="DefaultLimit"/> is used.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The Following feed response, or null when the upstream call to UserProfile failed (BFF should then return 502).</returns>
    public async Task<FollowingFeedResponse?> ComposeAsync(Guid identityId, int? requestedLimit, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(identityId, Guid.Empty);

        var limit = ClampLimit(requestedLimit);

        using var followingResponse = await _userProfileClient.GetAsync($"/api/users/{identityId}/following", accessToken: null, cancellationToken);
        if (followingResponse is null)
        {
            _logger.LogWarning("Following feed composer: UserProfile downstream call failed. TraceId will be set by the BFF endpoint layer.");
            return null;
        }
        if (!followingResponse.IsSuccessStatusCode)
        {
            return new FollowingFeedResponse([], limit);
        }

        var following = await followingResponse.Content.ReadFromJsonAsync<UserProfileListResponse>(cancellationToken: cancellationToken);
        if (following is null)
        {
            return new FollowingFeedResponse([], limit);
        }

        if (following.Items.Count == 0)
        {
            return new FollowingFeedResponse([], limit);
        }

        var queryItems = following.Items
            .Select(profile => new KeyValuePair<string, string?>("authorId", profile.IdentityId.ToString()))
            .Append(new KeyValuePair<string, string?>("limit", limit.ToString(CultureInfo.InvariantCulture)))
            .Append(new KeyValuePair<string, string?>("includeHidden", bool.FalseString));
        var postPathAndQuery = $"/api/posts/search{QueryString.Create(queryItems)}";
        using var postsResponse = await _postClient.ForwardGetAsync(postPathAndQuery, identityId, roles: Array.Empty<string>(), cancellationToken);
        if (postsResponse is null)
        {
            return new FollowingFeedResponse([], limit);
        }
        if (!postsResponse.IsSuccessStatusCode)
        {
            return new FollowingFeedResponse([], limit);
        }

        var authorPosts = await postsResponse.Content.ReadFromJsonAsync<PostSearchResponse>(cancellationToken: cancellationToken);
        if (authorPosts is null)
        {
            return new FollowingFeedResponse([], limit);
        }

        var items = authorPosts.Items
            .GroupBy(post => post.Id)
            .Select(group => group.First())
            .OrderByDescending(post => post.CreatedAtUtc)
            .ThenBy(post => post.Id)
            .Take(limit)
            .ToArray();

        return new FollowingFeedResponse(items, limit);
    }

    private static int ClampLimit(int? requestedLimit)
    {
        if (!requestedLimit.HasValue)
        {
            return DefaultLimit;
        }

        return Math.Clamp(requestedLimit.Value, 1, MaxLimit);
    }
}

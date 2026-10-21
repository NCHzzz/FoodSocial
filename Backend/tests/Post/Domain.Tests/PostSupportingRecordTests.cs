using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Domain.Tests;

public sealed class PostCommentRecordTests
{
    [Fact]
    public void NewRecord_ShouldStartVisible()
    {
        var comment = new PostCommentRecord
        {
            Id = Guid.NewGuid(),
            PostId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Body = "Looks great!"
        };

        comment.Status.Should().Be(CommentStatus.Visible);
        comment.IsVisible.Should().BeTrue();
    }
}

public sealed class PostMediaUploadRecordTests
{
    [Fact]
    public void MarkAttached_ShouldThrow_WhenPostIdIsEmpty()
    {
        var upload = new PostMediaUploadRecord
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Url = "https://cdn.foodsocial/uploads/x.jpg",
            ThumbnailUrl = "https://cdn.foodsocial/uploads/x-thumb.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            CleanupDueAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        Action act = () => upload.MarkAttached(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Post id is required.*");
    }

    [Fact]
    public void MarkAttached_ShouldTransitionToAttached()
    {
        var upload = new PostMediaUploadRecord
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Url = "https://cdn.foodsocial/uploads/x.jpg",
            ThumbnailUrl = "https://cdn.foodsocial/uploads/x-thumb.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            CleanupDueAtUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        var postId = Guid.NewGuid();
        upload.MarkAttached(postId);

        upload.Status.Should().Be(PostMediaUploadStatus.Attached);
        upload.AttachedPostId.Should().Be(postId);
        upload.AttachedAtUtc.Should().NotBeNull();
    }
}

public sealed class PostNotificationRecordTests
{
    [Fact]
    public void MarkRead_ShouldBeIdempotent()
    {
        var notification = new PostNotificationRecord
        {
            Id = Guid.NewGuid(),
            RecipientId = Guid.NewGuid(),
            ActorId = Guid.NewGuid(),
            Type = "like",
            TargetType = "post",
            TargetId = Guid.NewGuid(),
            Message = "Someone liked your post"
        };

        notification.MarkRead();
        var firstReadAt = notification.ReadAtUtc;

        notification.MarkRead();

        notification.ReadAtUtc.Should().Be(firstReadAt);
        notification.Status.Should().Be(NotificationStatus.Read);
    }

    [Fact]
    public void NewRecord_ShouldStartUnread()
    {
        var notification = new PostNotificationRecord
        {
            Id = Guid.NewGuid(),
            RecipientId = Guid.NewGuid(),
            ActorId = Guid.NewGuid(),
            Type = "comment",
            TargetType = "post",
            TargetId = Guid.NewGuid(),
            Message = "New comment"
        };

        notification.Status.Should().Be(NotificationStatus.Unread);
        notification.ReadAtUtc.Should().BeNull();
    }
}

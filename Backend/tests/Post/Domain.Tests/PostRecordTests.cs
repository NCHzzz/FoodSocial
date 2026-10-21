using FoodSocial.Post.Domain;

namespace FoodSocial.Post.Domain.Tests;

public sealed class PostRecordTests
{
    private static readonly Guid ValidAuthorId = Guid.NewGuid();
    private static readonly Guid ValidDishId = Guid.NewGuid();

    private static PostMediaRecord BuildPrimaryMedia() =>
        PostMediaRecord.Create("https://cdn.foodsocial/posts/x.jpg", "image", null, null, true, 0);

    [Fact]
    public void Create_ShouldThrow_WhenAuthorIdIsEmpty()
    {
        Action act = () => PostRecord.Create(Guid.Empty, ValidDishId, "caption", [BuildPrimaryMedia()], null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Author id is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenDishIdIsEmpty()
    {
        Action act = () => PostRecord.Create(ValidAuthorId, Guid.Empty, "caption", [BuildPrimaryMedia()], null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Dish id is required.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenCaptionIsEmpty()
    {
        Action act = () => PostRecord.Create(ValidAuthorId, ValidDishId, "  ", [BuildPrimaryMedia()], null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Caption is required.*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public void Create_ShouldThrow_WhenRatingOutOfRange(int rating)
    {
        Action act = () => PostRecord.Create(ValidAuthorId, ValidDishId, "caption", [BuildPrimaryMedia()], null, rating, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("Rating must be between 1 and 5.*");
    }

    [Fact]
    public void Create_ShouldThrow_WhenNoMediaProvided()
    {
        Action act = () => PostRecord.Create(ValidAuthorId, ValidDishId, "caption", Array.Empty<PostMediaRecord>(), null, null, null, null, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage("At least one primary media URL is required.*");
    }

    [Fact]
    public void Create_ShouldStartAsPublished()
    {
        var post = PostRecord.Create(ValidAuthorId, ValidDishId, "caption", [BuildPrimaryMedia()], null, 5, null, null, null);

        post.Status.Should().Be(PostStatus.Published);
        post.IsHidden.Should().BeFalse();
    }

    [Fact]
    public void Hide_ShouldSetStatusToHidden()
    {
        var post = PostRecord.Create(ValidAuthorId, ValidDishId, "caption", [BuildPrimaryMedia()], null, null, null, null, null);

        post.Hide();

        post.Status.Should().Be(PostStatus.Hidden);
        post.IsHidden.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldNormalizeTags()
    {
        var post = PostRecord.Create(
            ValidAuthorId,
            ValidDishId,
            "caption",
            [BuildPrimaryMedia()],
            null,
            null,
            null,
            new[] { "#Spicy", "spicy", "Bun Bo", "  bun-bo  " },
            null);

        post.Tags.Should().BeEquivalentTo(new[] { "spicy", "bun bo", "bun-bo" });
    }

    [Fact]
    public void Create_ShouldTreatEmptyPlaceIdAsNull()
    {
        var post = PostRecord.Create(ValidAuthorId, ValidDishId, "caption", [BuildPrimaryMedia()], Guid.Empty, null, null, null, null);

        post.PlaceId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldTrimCaption()
    {
        var post = PostRecord.Create(ValidAuthorId, ValidDishId, "  caption  ", [BuildPrimaryMedia()], null, null, null, null, null);

        post.Caption.Should().Be("caption");
    }
}

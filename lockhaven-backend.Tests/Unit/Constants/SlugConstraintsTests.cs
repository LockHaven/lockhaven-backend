using lockhaven_backend.Constants;

namespace lockhaven_backend.Tests.Unit.Constants;

public class SlugConstraintsTests
{
    [Theory]
    [InlineData("a")]
    [InlineData("my-app")]
    [InlineData("env-1-staging")]
    [InlineData("abc123")]
    public void IsValid_AcceptsNormalizedKebabSlugs(string slug) =>
        Assert.True(SlugConstraints.IsValid(slug));

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("my--app")]
    [InlineData("a b")]
    [InlineData("a--b")]
    [InlineData("-ab")]
    [InlineData("ab-")]
    [InlineData("ab_c")]
    [InlineData("../x")]
    public void IsValid_RejectsInvalidSlugs(string slug) =>
        Assert.False(SlugConstraints.IsValid(SlugConstraints.NormalizeSlug(slug)));

    [Fact]
    public void NormalizeSlug_TrimsAndLowercases() =>
        Assert.Equal("my-proj", SlugConstraints.NormalizeSlug("  My-Proj  "));

    [Fact]
    public void OptionalPattern_AllowsEmptyString() =>
        Assert.Matches(SlugConstraints.OptionalPattern, string.Empty);
}

using FluentAssertions;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Domain.Tests;

public sealed class IdentityAppRolesTests
{
    [Fact]
    public void Normalize_ValidCanonicalRole_ReturnsNormalizedRole()
    {
        var normalizedRole = IdentityAppRoles.Normalize("ADMIN");

        normalizedRole.Should().Be(IdentityAppRoles.Admin);
    }

    [Fact]
    public void Normalize_UnsupportedRole_ThrowsArgumentOutOfRangeException()
    {
        var act = () => IdentityAppRoles.Normalize("super-admin");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

using FluentAssertions;
using FoodSocial.Identity.Application;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Application.Tests;

public sealed class IdentityLifecyclePolicyTests
{
    [Theory]
    [InlineData(IdentityLifecycleState.PendingBootstrap, true, true, true, true)]
    [InlineData(IdentityLifecycleState.Active, true, true, true, false)]
    [InlineData(IdentityLifecycleState.Suspended, false, false, false, false)]
    [InlineData(IdentityLifecycleState.Deleted, false, false, false, false)]
    public void Describe_ReturnsExpectedCapabilities(
        IdentityLifecycleState state,
        bool loginAllowed,
        bool refreshAllowed,
        bool roleResolutionAllowed,
        bool bootstrapPending)
    {
        var policy = IdentityLifecyclePolicy.Describe(state);

        policy.CurrentState.Should().Be(state);
        policy.LoginAllowed.Should().Be(loginAllowed);
        policy.RefreshAllowed.Should().Be(refreshAllowed);
        policy.RoleResolutionAllowed.Should().Be(roleResolutionAllowed);
        policy.DownstreamBootstrapPending.Should().Be(bootstrapPending);
    }

    [Fact]
    public void Describe_UsesCanonicalDomainRules()
    {
        var policy = IdentityLifecyclePolicy.Describe(IdentityLifecycleState.Active);

        policy.Notes.Should().ContainSingle(note => note == "Active identities can sign in, refresh sessions, and resolve roles.");
    }
}

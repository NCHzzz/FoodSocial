using FluentAssertions;
using FoodSocial.Identity.Domain;
using Xunit;

namespace FoodSocial.Identity.Domain.Tests;

public sealed class IdentityUserLifecycleTests
{
    [Theory]
    [InlineData(IdentityLifecycleState.PendingBootstrap, IdentityLifecycleTransition.Activate, IdentityLifecycleState.Active)]
    [InlineData(IdentityLifecycleState.PendingBootstrap, IdentityLifecycleTransition.Suspend, IdentityLifecycleState.Suspended)]
    [InlineData(IdentityLifecycleState.PendingBootstrap, IdentityLifecycleTransition.Delete, IdentityLifecycleState.Deleted)]
    [InlineData(IdentityLifecycleState.Active, IdentityLifecycleTransition.Suspend, IdentityLifecycleState.Suspended)]
    [InlineData(IdentityLifecycleState.Active, IdentityLifecycleTransition.Delete, IdentityLifecycleState.Deleted)]
    [InlineData(IdentityLifecycleState.Suspended, IdentityLifecycleTransition.Activate, IdentityLifecycleState.Active)]
    [InlineData(IdentityLifecycleState.Suspended, IdentityLifecycleTransition.Delete, IdentityLifecycleState.Deleted)]
    public void TransitionTo_AllowsSupportedTransitions(
        IdentityLifecycleState initialState,
        IdentityLifecycleTransition transition,
        IdentityLifecycleState expectedState)
    {
        var user = CreateUser(initialState);

        user.TransitionTo(transition);

        user.State.Should().Be(expectedState);
    }

    [Theory]
    [InlineData(IdentityLifecycleState.Active, IdentityLifecycleTransition.Activate)]
    [InlineData(IdentityLifecycleState.Suspended, IdentityLifecycleTransition.Suspend)]
    [InlineData(IdentityLifecycleState.Deleted, IdentityLifecycleTransition.Activate)]
    [InlineData(IdentityLifecycleState.Deleted, IdentityLifecycleTransition.Suspend)]
    [InlineData(IdentityLifecycleState.Deleted, IdentityLifecycleTransition.Delete)]
    public void TransitionTo_RejectsUnsupportedTransitions(
        IdentityLifecycleState initialState,
        IdentityLifecycleTransition transition)
    {
        var user = CreateUser(initialState);

        var act = () => user.TransitionTo(transition);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"Transition '{transition}' is not allowed from state '{initialState}'.");
    }

    [Theory]
    [InlineData(IdentityLifecycleState.PendingBootstrap, true, true, true, true)]
    [InlineData(IdentityLifecycleState.Active, true, true, true, false)]
    [InlineData(IdentityLifecycleState.Suspended, false, false, false, false)]
    [InlineData(IdentityLifecycleState.Deleted, false, false, false, false)]
    public void StateRules_ExposeLifecycleCapabilities(
        IdentityLifecycleState state,
        bool loginAllowed,
        bool refreshAllowed,
        bool roleResolutionAllowed,
        bool bootstrapPublicationRequired)
    {
        var rules = IdentityLifecycleRules.For(state);

        rules.LoginAllowed.Should().Be(loginAllowed);
        rules.RefreshAllowed.Should().Be(refreshAllowed);
        rules.RoleResolutionAllowed.Should().Be(roleResolutionAllowed);
        rules.DownstreamBootstrapPublicationRequired.Should().Be(bootstrapPublicationRequired);
    }

    [Fact]
    public void For_RejectsUndefinedLifecycleState()
    {
        var act = () => IdentityLifecycleRules.For((IdentityLifecycleState)999);

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Unsupported lifecycle state.*");
    }

    private static IdentityUser CreateUser(IdentityLifecycleState state)
    {
        var user = new IdentityUser("subject-123", "chef", "chef@example.com");

        user.State.Should().Be(IdentityLifecycleState.PendingBootstrap);

        return state switch
        {
            IdentityLifecycleState.PendingBootstrap => user,
            IdentityLifecycleState.Active => Activate(user),
            IdentityLifecycleState.Suspended => Suspend(user),
            IdentityLifecycleState.Deleted => Delete(user),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, "Unsupported lifecycle state.")
        };
    }

    private static IdentityUser Activate(IdentityUser user)
    {
        user.TransitionTo(IdentityLifecycleTransition.Activate);
        return user;
    }

    private static IdentityUser Suspend(IdentityUser user)
    {
        Activate(user);
        user.TransitionTo(IdentityLifecycleTransition.Suspend);
        return user;
    }

    private static IdentityUser Delete(IdentityUser user)
    {
        user.TransitionTo(IdentityLifecycleTransition.Delete);
        return user;
    }
}

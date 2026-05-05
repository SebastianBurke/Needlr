using System.Reflection;
using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

namespace Needlr.Architecture.Tests;

/// <summary>
/// Enforces the layering rules in <c>docs/ARCHITECTURE.md</c>. Every rule below is binding;
/// breaking one means a project reference graph or namespace placement has drifted, not a
/// false positive — fix the offending code, not the test.
/// </summary>
public class LayeringTests
{
    private static readonly Assembly Domain         = typeof(Needlr.Domain.Identity.Artist).Assembly;
    private static readonly Assembly Application    = typeof(Needlr.Application.Abstractions.IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(Needlr.Infrastructure.Persistence.NeedlrDbContext).Assembly;
    private static readonly Assembly Api            = typeof(Needlr.Api.ApiAssemblyMarker).Assembly;
    private static readonly Assembly Web            = typeof(Needlr.Web.WebAssemblyMarker).Assembly;
    private static readonly Assembly Contracts      = typeof(Needlr.Contracts.ContractsAssemblyMarker).Assembly;

    [Fact]
    public void Domain_HasNoDependenciesOnOtherLayers()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Needlr.Application",
                "Needlr.Infrastructure",
                "Needlr.Api",
                "Needlr.Web",
                "Needlr.Contracts")
            .GetResult();

        AssertSatisfied(result, "Domain must not depend on any other Needlr layer.");
    }

    [Fact]
    public void Application_DependsOnDomainOnly()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Needlr.Infrastructure",
                "Needlr.Api",
                "Needlr.Web",
                "Needlr.Contracts")
            .GetResult();

        AssertSatisfied(result, "Application may only depend on Domain among Needlr layers.");
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApiOrWebOrContracts()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Needlr.Api",
                "Needlr.Web",
                "Needlr.Contracts")
            .GetResult();

        AssertSatisfied(result, "Infrastructure may only depend on Application and Domain.");
    }

    [Fact]
    public void Web_DependsOnContractsOnly_NotOnServerLayers()
    {
        var result = Types.InAssembly(Web)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Needlr.Domain",
                "Needlr.Application",
                "Needlr.Infrastructure",
                "Needlr.Api")
            .GetResult();

        AssertSatisfied(result, "Web is the Blazor client and must only reference Contracts.");
    }

    [Fact]
    public void Contracts_HasNoInternalLayerDependencies()
    {
        var result = Types.InAssembly(Contracts)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Needlr.Domain",
                "Needlr.Application",
                "Needlr.Infrastructure",
                "Needlr.Api",
                "Needlr.Web")
            .GetResult();

        AssertSatisfied(result, "Contracts is shared client/server DTOs and must depend on no Needlr layer.");
    }

    [Fact]
    public void Domain_Entities_AreSealed()
    {
        // Constructor-validated entities should be sealed unless there's a reason to inherit;
        // we have no such reason yet, and "all sealed" is the safest default.
        var result = Types.InAssembly(Domain)
            .That()
            .ResideInNamespaceMatching("Needlr.Domain.(Identity|Studios|Verification|Portfolio|Bookings|Availability|Messaging)")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        AssertSatisfied(result, "Domain entity classes must be sealed.");
    }

    private static void AssertSatisfied(TestResult result, string because)
    {
        var failing = result.FailingTypeNames is null
            ? string.Empty
            : string.Join(", ", result.FailingTypeNames);
        result.IsSuccessful.Should().BeTrue(
            $"{because} Failing types: [{failing}]");
    }
}

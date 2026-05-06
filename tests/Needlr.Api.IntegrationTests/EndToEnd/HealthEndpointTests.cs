using System.Net;
using FluentAssertions;
using Needlr.Api.IntegrationTests.Fixtures;
using Xunit;

namespace Needlr.Api.IntegrationTests.EndToEnd;

public sealed class HealthEndpointTests : IClassFixture<WebAppFixture>
{
    private readonly WebAppFixture _fixture;

    public HealthEndpointTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Health_Anonymous_ReturnsHealthy()
    {
        var anonymous = _fixture.Factory.CreateClient();
        var response = await anonymous.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("Healthy");
    }
}

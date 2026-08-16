using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using HotelReservation.Application.DTOs;
using Xunit;

namespace HotelReservation.Tests.Integration.Authentication;

public class AuthenticationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_Login_And_Access_Mine()
    {
        var client = _factory.CreateClient();

        var register = new { Email = "integ@example.com", Password = "P@ssw0rd!", FirstName = "I", LastName = "T" };
        var resp = await client.PostAsJsonAsync("/api/account/register", register);
        resp.EnsureSuccessStatusCode();

        var content = await resp.Content.ReadFromJsonAsync<AuthenticationResponse>();
        string token = content?.Token;
        token.Should().NotBeNullOrEmpty();

        var auth = _factory.CreateClient();
        auth.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var mine = await auth.GetAsync("/api/reservations/mine");
        mine.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}

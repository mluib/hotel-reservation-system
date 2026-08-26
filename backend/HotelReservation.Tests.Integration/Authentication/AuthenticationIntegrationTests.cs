using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotelReservation.Application.DTOs;
using HotelReservation.Infrastructure.Persistence;
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

    // Reproduces a real bug: deleting a Customer row directly (bypassing DeleteCustomer,
    // e.g. straight through a DB tool) used to still leave that Identity login able to
    // sign in -- it just failed later, confusingly, the first time anything needed the
    // (now-missing) Customer profile. Login itself should reject this account instead.
    [Fact]
    public async Task Login_CustomerRoleWithNoLinkedCustomerRecord_Returns401()
    {
        var email = $"orphaned-{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd!";

        var client = _factory.CreateClient();
        var register = new { Email = email, Password = password, FirstName = "No", LastName = "Profile" };
        (await client.PostAsJsonAsync("/api/account/register", register)).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var identityUser = await userManager.FindByEmailAsync(email);
            // Email is a value object (EmailAddress) -- SQLite's LINQ provider (used by
            // these tests) can't translate a query into its .Value the way SQL Server can,
            // so look the row up by IdentityUserId (a plain string) instead.
            var customer = await db.Customers.SingleAsync(c => c.IdentityUserId == identityUser!.Id);
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/account/login", new { Email = email, Password = password });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // The other half of the same fix: deleting a customer through the real admin path
    // should itself prevent this state from arising, by revoking the Identity login too.
    [Fact]
    public async Task DeleteCustomer_ThroughAdminApi_AlsoRevokesLogin()
    {
        var email = $"deleted-{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd!";

        var customerClient = _factory.CreateClient();
        var register = new { Email = email, Password = password, FirstName = "To", LastName = "Delete" };
        var registerResponse = await customerClient.PostAsJsonAsync("/api/account/register", register);
        registerResponse.EnsureSuccessStatusCode();
        var registerBody = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        customerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registerBody!.Token);

        var meResponse = await customerClient.GetAsync("/api/customers/mine");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var adminClient = await SeedAdminAndLoginAsync();
        var deleteResponse = await adminClient.DeleteAsync($"/api/customers/{me!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginAfterDelete = await _factory.CreateClient()
            .PostAsJsonAsync("/api/account/login", new { Email = email, Password = password });

        loginAfterDelete.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Backend-hardening pass: SignInManager.CheckPasswordSignInAsync now enforces
    // lockout, previously inert since AuthService called UserManager.CheckPasswordAsync
    // directly (see docs/decisions.md). Asserts lockout actually engages after
    // MaxFailedAccessAttempts wrong passwords and then rejects even the correct one --
    // rather than waiting out the real 5-minute DefaultLockoutTimeSpan, which would make
    // this test unacceptably slow.
    [Fact]
    public async Task Login_AfterMaxFailedAttempts_LocksOutAccountEvenWithCorrectPassword()
    {
        var email = $"lockout-{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd!";

        var client = _factory.CreateClient();
        var register = new { Email = email, Password = password, FirstName = "Lock", LastName = "Out" };
        (await client.PostAsJsonAsync("/api/account/register", register)).EnsureSuccessStatusCode();

        // Program.cs configures MaxFailedAccessAttempts = 5 -- exhaust it.
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            var failed = await client.PostAsJsonAsync("/api/account/login", new { Email = email, Password = "WrongPassword!1" });
            failed.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByEmailAsync(email);
            (await userManager.IsLockedOutAsync(user!)).Should().BeTrue();
        }

        // Even the correct password is rejected now -- proves lockout is actually
        // enforced by the login path itself, not just recorded in the database.
        var correctPasswordAttempt = await client.PostAsJsonAsync("/api/account/login", new { Email = email, Password = password });
        correctPasswordAttempt.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Mirrors AuthorizationIntegrationTests' own helper -- kept local rather than shared
    // since this is the only other test class that needs an Admin token.
    private async Task<HttpClient> SeedAdminAndLoginAsync()
    {
        var email = $"admin-{Guid.NewGuid()}@example.com";
        const string password = "P@ssw0rd!";

        using (var scope = _factory.Services.CreateScope())
        {
            var services = scope.ServiceProvider;

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
                await roleManager.CreateAsync(new IdentityRole("Admin"));

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var user = new IdentityUser { UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user, password);
            createResult.Succeeded.Should().BeTrue();
            await userManager.AddToRoleAsync(user, "Admin");
        }

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/account/login", new { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();

        var body = await loginResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }
}

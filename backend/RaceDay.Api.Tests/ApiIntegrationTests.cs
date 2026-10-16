using System;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.DTOs.Auth;
using Xunit;

namespace RaceDay.Api.Tests;

public sealed class ApiIntegrationTests
{
    [Fact]
    public async Task RegistrationAndLogin_ValidateInputsAndIssueToken()
    {
        using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();

        var invalidRole = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Test",
            lastName = "User",
            email = "invalid-role@example.com",
            password = "valid-test-password",
            role = "Administrator"
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidRole.StatusCode);

        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Test",
            lastName = "Participant",
            email = "Test.Participant@example.com",
            password = "valid-test-password",
            role = "Participant"
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.DoesNotContain("PasswordHash", await registration.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "test.participant@example.com",
            password = "valid-test-password"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth?.AccessToken));

        var badLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "test.participant@example.com",
            password = "incorrect-password"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, badLogin.StatusCode);
    }

    [Fact]
    public async Task EventEndpoints_EnforceAuthenticationAndOrganiserOwnership()
    {
        using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var organiserToken = await RegisterAndLogin(client, "owner@example.com", "Organiser");
        var otherOrganiserToken = await RegisterAndLogin(client, "other@example.com", "Organiser");
        var participantToken = await RegisterAndLogin(client, "participant@example.com", "Participant");
        var eventBody = new
        {
            name = "Test Race",
            description = "Integration test event",
            eventType = "Running",
            location = "Park",
            city = "Cape Town",
            province = "Western Cape",
            distanceKm = 10m,
            eventDateTime = DateTime.UtcNow.AddDays(30),
            status = "Open",
            maxParticipants = 100
        };

        var anonymousCreate = await client.PostAsJsonAsync("/api/v1/events", eventBody);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousCreate.StatusCode);

        using var participantRequest = Authorized(HttpMethod.Post, "/api/v1/events", participantToken, eventBody);
        var participantCreate = await client.SendAsync(participantRequest);
        Assert.Equal(HttpStatusCode.Forbidden, participantCreate.StatusCode);

        using var createRequest = Authorized(HttpMethod.Post, "/api/v1/events", organiserToken, eventBody);
        var create = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var eventData = await create.Content.ReadFromJsonAsync<EventResponse>();
        Assert.NotNull(eventData);

        using var updateOtherOwner = Authorized(HttpMethod.Put, $"/api/v1/events/{eventData!.EventId}", otherOrganiserToken, new
        {
            name = "Not the owner"
        });
        var forbiddenUpdate = await client.SendAsync(updateOtherOwner);
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenUpdate.StatusCode);

        using var updateOwner = Authorized(HttpMethod.Put, $"/api/v1/events/{eventData.EventId}", organiserToken, new
        {
            name = "Updated Test Race"
        });
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(updateOwner)).StatusCode);

        using var deleteOwner = Authorized(HttpMethod.Delete, $"/api/v1/events/{eventData.EventId}", organiserToken);
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(deleteOwner)).StatusCode);
    }

    [Fact]
    public async Task EnrolmentAndResultFlow_EnforcesParticipantAndOrganiserRules()
    {
        using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var organiserToken = await RegisterAndLogin(client, "race-owner@example.com", "Organiser");
        var anotherOrganiserToken = await RegisterAndLogin(client, "race-not-owner@example.com", "Organiser");
        var participantToken = await RegisterAndLogin(client, "runner@example.com", "Participant");
        var eventId = await CreateEvent(client, organiserToken);

        using var categoryRequest = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/categories", organiserToken, new
        {
            name = "10 km",
            distanceKm = 10m,
            minAge = 18,
            maxAge = 99
        });
        var categoryResponse = await client.SendAsync(categoryRequest);
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        using var enrolRequest = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/enrolments", participantToken, new
        {
            categoryId = category!.CategoryId
        });
        var enrolResponse = await client.SendAsync(enrolRequest);
        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.NotNull(enrolment);
        Assert.Equal("Pending", enrolment!.PaymentStatus);

        using var duplicateRequest = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/enrolments", participantToken, new
        {
            categoryId = category.CategoryId
        });
        Assert.Equal(HttpStatusCode.Conflict, (await client.SendAsync(duplicateRequest)).StatusCode);

        using var otherOrganizerList = Authorized(HttpMethod.Get, $"/api/v1/events/{eventId}/enrolments", anotherOrganiserToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(otherOrganizerList)).StatusCode);

        using var resultRequest = Authorized(HttpMethod.Post, $"/api/v1/enrolments/{enrolment.EnrolmentId}/result", organiserToken, new
        {
            finishTimeSeconds = 3600,
            positionOverall = 1,
            positionCategory = 1,
            positionGender = 1,
            status = "Finished"
        });
        var resultResponse = await client.SendAsync(resultRequest);
        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);

        using var publicResults = await client.GetAsync($"/api/v1/events/{eventId}/results");
        Assert.Equal(HttpStatusCode.OK, publicResults.StatusCode);
    }

    [Fact]
    public async Task ProfileRouteAndHistoryEndpoints_ReturnOwnedData()
    {
        using var factory = new RaceDayApiFactory();
        using var client = factory.CreateClient();
        var organiserToken = await RegisterAndLogin(client, "organiser-profile@example.com", "Organiser");
        var participantToken = await RegisterAndLogin(client, "participant-profile@example.com", "Participant");
        var eventId = await CreateEvent(client, organiserToken);

        using var createCategory = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/categories", organiserToken, new
        {
            name = "5 km",
            distanceKm = 5m
        });
        var categoryResponse = await client.SendAsync(createCategory);
        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.NotNull(category);

        using var createEnrolment = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/enrolments", participantToken, new
        {
            categoryId = category!.CategoryId
        });
        Assert.Equal(HttpStatusCode.Created, (await client.SendAsync(createEnrolment)).StatusCode);

        using var createRoute = Authorized(HttpMethod.Post, $"/api/v1/events/{eventId}/routes", organiserToken, new
        {
            name = "Main Route",
            distanceKm = 5m,
            gpxUrl = "https://example.test/route.gpx",
            elevationGainMeters = 100
        });
        var routeResponse = await client.SendAsync(createRoute);
        Assert.Equal(HttpStatusCode.Created, routeResponse.StatusCode);
        var route = await routeResponse.Content.ReadFromJsonAsync<RouteResponse>();
        Assert.NotNull(route);

        using var participantRouteUpdate = Authorized(HttpMethod.Put, $"/api/v1/routes/{route!.RouteId}", participantToken, new
        {
            name = "Changed"
        });
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(participantRouteUpdate)).StatusCode);

        using var participantClient = factory.CreateClient();
        participantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", participantToken);
        var profile = await participantClient.GetAsync("/api/v1/users/me");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        var history = await participantClient.GetAsync("/api/v1/users/me/history");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        Assert.Contains($"\"eventId\":{eventId}", await history.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> RegisterAndLogin(HttpClient client, string email, string role)
    {
        var registration = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            firstName = "Integration",
            lastName = "User",
            email,
            password = "integration-test-password",
            role
        });
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "integration-test-password"
        });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var response = await login.Content.ReadFromJsonAsync<AuthResponse>();
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(response!.AccessToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "RaceDayTests",
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(RaceDayApiFactory.JwtKey)),
            ValidateLifetime = true
        }, out _);
        return response.AccessToken;
    }

    private static async Task<int> CreateEvent(HttpClient client, string token)
    {
        using var request = Authorized(HttpMethod.Post, "/api/v1/events", token, new
        {
            name = "History Race",
            eventType = "Running",
            location = "Park",
            city = "Cape Town",
            province = "Western Cape",
            distanceKm = 10m,
            eventDateTime = DateTime.UtcNow.AddDays(30),
            status = "Open"
        });
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EventResponse>())!.EventId;
    }

    private static HttpRequestMessage Authorized(HttpMethod method, string path, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private sealed class EventResponse
    {
        public int EventId { get; init; }
    }

    private sealed class CategoryResponse
    {
        public int CategoryId { get; init; }
    }

    private sealed class EnrolmentResponse
    {
        public int EnrolmentId { get; init; }
        public string PaymentStatus { get; init; } = null!;
    }

    private sealed class RouteResponse
    {
        public int RouteId { get; init; }
    }
}

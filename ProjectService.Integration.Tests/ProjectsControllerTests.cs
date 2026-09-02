using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace ProjectService.Integration.Tests;

/// <summary>
/// Интеграционные тесты для API.
/// Проверяют HTTP-эндпоинты через WebApplicationFactory.
/// </summary>
public class ProjectsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _httpClient;

    public ProjectsControllerTests(WebApplicationFactory<Program> factory)
    {
        _httpClient = factory.CreateClient();
    }

    /// <summary>
    /// Проверяет, что API возвращает 200 OK при запросе /api/projects.
    /// </summary>
    [Fact]
    public async Task GetAllProjects_ReturnsOk()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/projects");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    /// <summary>
    /// Проверяет, что Swagger UI доступен.
    /// </summary>
    [Fact]
    public async Task SwaggerEndpoint_ReturnsOk()
    {
        // Act
        var response = await _httpClient.GetAsync("/swagger");

        // Assert
        // Swagger может перенаправлять на /swagger/index.html
        Assert.True(response.StatusCode == System.Net.HttpStatusCode.OK ||
                    response.StatusCode == System.Net.HttpStatusCode.Redirect);
    }
}

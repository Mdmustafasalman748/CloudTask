using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CloudTask.Api.Common;
using CloudTask.Api.Dtos;

namespace CloudTask.Tests.Api;

public class ProjectsPagingTests : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private HttpClient _client = null!;

    public ProjectsPagingTests(ApiFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    // Creates 5 projects sharing a unique lowercase token, so each test sees only its own data
    private async Task<string> SeedFiveAsync()
    {
        var token = Guid.NewGuid().ToString("N");

        for (var i = 1; i <= 5; i++)
        {
            var response = await _client.PostAsJsonAsync(
                "/api/projects", new { name = $"{token}-{i:D2}" });
            response.EnsureSuccessStatusCode();
        }

        return token;
    }

    private Task<PagedResult<ProjectResponse>?> GetPageAsync(string url)
    {
        return _client.GetFromJsonAsync<PagedResult<ProjectResponse>>(url);
    }

    [Fact]
    public async Task Paging_ReturnsRequestedSliceAndTotals()
    {
        var token = await SeedFiveAsync();

        var page2 = await GetPageAsync(
            $"/api/projects?search={token}&page=2&pageSize=2&sortBy=name");

        Assert.Equal(2, page2!.Items.Count);
        Assert.Equal(5, page2.TotalCount);
        Assert.Equal(3, page2.TotalPages);
        Assert.Equal($"{token}-03", page2.Items[0].Name);
    }

    [Fact]
    public async Task Paging_LastPageIsPartial()
    {
        var token = await SeedFiveAsync();

        var page3 = await GetPageAsync($"/api/projects?search={token}&page=3&pageSize=2");

        Assert.Single(page3!.Items);
    }

    [Fact]
    public async Task Sorting_ByNameDescending_ReturnsReverseOrder()
    {
        var token = await SeedFiveAsync();

        var result = await GetPageAsync(
            $"/api/projects?search={token}&sortBy=name&descending=true");

        var names = result!.Items.Select(p => p.Name).ToList();
        Assert.Equal(names.OrderByDescending(n => n, StringComparer.Ordinal), names);
    }

    [Fact]
    public async Task PageSizeAboveCap_Returns400()
    {
        var response = await _client.GetAsync("/api/projects?pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSortField_Returns400()
    {
        var response = await _client.GetAsync("/api/projects?sortBy=password");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
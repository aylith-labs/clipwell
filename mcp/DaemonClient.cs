using System.Net.Http.Json;
using Clipwell.Protocol;

namespace Clipwell.Mcp;

/// <summary>HTTP client for the Clipwell daemon's REST API.</summary>
public sealed class DaemonClient
{
    private readonly HttpClient _http;

    public DaemonClient()
    {
        var baseUrl = Environment.GetEnvironmentVariable("CLIPWELL_API") ?? "http://127.0.0.1:8787";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(5) };
    }

    private sealed record PageResponse(List<ClipItem> Items);

    private sealed record ClearResponse(int Deleted);

    public async Task<IReadOnlyList<ClipItem>> GetPageAsync(int limit, string? before = null)
    {
        var url = before is null
            ? $"/api/clipboard?limit={limit}"
            : $"/api/clipboard?limit={limit}&before={Uri.EscapeDataString(before)}";
        var page = await _http.GetFromJsonAsync<PageResponse>(url);
        return page?.Items ?? [];
    }

    /// <summary>Searches the whole history server-side, not just the newest page.</summary>
    public async Task<IReadOnlyList<ClipItem>> SearchAsync(string query, int limit)
    {
        var url = $"/api/clipboard/search?q={Uri.EscapeDataString(query)}&limit={limit}";
        var page = await _http.GetFromJsonAsync<PageResponse>(url);
        return page?.Items ?? [];
    }

    public async Task<ClipItem?> GetItemAsync(string timestamp)
    {
        using var response = await _http.GetAsync($"/api/clipboard/item/{Uri.EscapeDataString(timestamp)}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ClipItem>();
    }

    /// <summary>Clears all history and returns how many rows the daemon deleted.</summary>
    public async Task<int> ClearAsync()
    {
        using var response = await _http.PostAsync("/api/clipboard/clear", null);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ClearResponse>();
        return body?.Deleted ?? 0;
    }
}

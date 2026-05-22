using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierLocalApiClient(
    HttpClient httpClient,
    IOptions<ZeroTierOptions> options)
{
    private readonly ZeroTierOptions _options = options.Value;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ZeroTierStatus?> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "/status", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierStatus>(JsonOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListNetworkIdsAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "/controller/network", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<ZeroTierNetwork?> GetNetworkAsync(string networkId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"/controller/network/{networkId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierNetwork>(JsonOptions, cancellationToken);
    }

    public async Task<ZeroTierNetwork?> CreateNetworkAsync(string name, CancellationToken cancellationToken = default)
    {
        var status = await GetStatusAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(status?.Address))
        {
            throw new InvalidOperationException("ZeroTier local node address is unavailable.");
        }

        using var request = await CreateRequestAsync(HttpMethod.Post, $"/controller/network/{status.Address}______", cancellationToken);
        request.Content = JsonContent.Create(new { name }, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierNetwork>(JsonOptions, cancellationToken);
    }

    public async Task<ZeroTierNetwork?> UpdateNetworkAsync(string networkId, object patch, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"/controller/network/{networkId}", cancellationToken);
        request.Content = JsonContent.Create(patch, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierNetwork>(JsonOptions, cancellationToken);
    }

    public async Task DeleteNetworkAsync(string networkId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"/controller/network/{networkId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<string>> ListMemberIdsAsync(string networkId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"/controller/network/{networkId}/member", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        using var json = JsonDocument.Parse(raw);
        return json.RootElement.ValueKind switch
        {
            JsonValueKind.Array => json.RootElement
                .EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToArray(),
            JsonValueKind.Object => json.RootElement
                .EnumerateObject()
                .Select(x => x.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToArray(),
            _ => []
        };
    }

    public async Task<ZeroTierMember?> GetMemberAsync(string networkId, string memberId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, $"/controller/network/{networkId}/member/{memberId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierMember>(JsonOptions, cancellationToken);
    }

    public async Task<ZeroTierMember?> UpdateMemberAsync(string networkId, string memberId, object patch, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Post, $"/controller/network/{networkId}/member/{memberId}", cancellationToken);
        request.Content = JsonContent.Create(patch, options: JsonOptions);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ZeroTierMember>(JsonOptions, cancellationToken);
    }

    public async Task DeleteMemberAsync(string networkId, string memberId, CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Delete, $"/controller/network/{networkId}/member/{memberId}", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<ZeroTierPeer>> ListPeersAsync(CancellationToken cancellationToken = default)
    {
        using var request = await CreateRequestAsync(HttpMethod.Get, "/peer", cancellationToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<ZeroTierPeer>>(JsonOptions, cancellationToken) ?? [];
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(HttpMethod method, string path, CancellationToken cancellationToken)
    {
        var baseUrl = $"http://127.0.0.1:{_options.Port}";
        var request = new HttpRequestMessage(method, baseUrl + path);
        var token = await ReadTokenAsync(cancellationToken);
        request.Headers.TryAddWithoutValidation("X-ZT1-Auth", token);
        return request;
    }

    private async Task<string> ReadTokenAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AuthTokenPath))
        {
            throw new InvalidOperationException("ZeroTier auth token path is not configured.");
        }

        var authTokenPath = ResolveAuthTokenPath();
        if (!File.Exists(authTokenPath))
        {
            throw new FileNotFoundException("ZeroTier auth token file was not found.", authTokenPath);
        }

        return (await File.ReadAllTextAsync(authTokenPath, cancellationToken)).Trim();
    }

    private string ResolveAuthTokenPath()
    {
        return Path.IsPathRooted(_options.AuthTokenPath)
            ? _options.AuthTokenPath
            : Path.GetFullPath(_options.AuthTokenPath);
    }
}

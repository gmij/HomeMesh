using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierLocalApiClient(HttpClient httpClient, IOptions<ZeroTierOptions> options)
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
        return await response.Content.ReadFromJsonAsync<List<string>>(JsonOptions, cancellationToken) ?? [];
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
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
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

        if (!File.Exists(_options.AuthTokenPath))
        {
            throw new FileNotFoundException("ZeroTier auth token file was not found.", _options.AuthTokenPath);
        }

        return (await File.ReadAllTextAsync(_options.AuthTokenPath, cancellationToken)).Trim();
    }
}

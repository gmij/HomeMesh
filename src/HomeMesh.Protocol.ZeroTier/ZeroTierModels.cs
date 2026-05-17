using System.Text.Json.Serialization;

namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierStatus
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("online")]
    public bool Online { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

public sealed class ZeroTierNetwork
{
    [JsonPropertyName("nwid")]
    public string? Nwid { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("private")]
    public bool Private { get; set; } = true;

    [JsonPropertyName("routes")]
    public List<ZeroTierRoute> Routes { get; set; } = [];

    [JsonPropertyName("ipAssignmentPools")]
    public List<ZeroTierIpAssignmentPool> IpAssignmentPools { get; set; } = [];

    [JsonPropertyName("dns")]
    public ZeroTierDns? Dns { get; set; }
}

public sealed class ZeroTierRoute
{
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    [JsonPropertyName("via")]
    public string? Via { get; set; }
}

public sealed class ZeroTierIpAssignmentPool
{
    [JsonPropertyName("ipRangeStart")]
    public string? IpRangeStart { get; set; }

    [JsonPropertyName("ipRangeEnd")]
    public string? IpRangeEnd { get; set; }
}

public sealed class ZeroTierDns
{
    [JsonPropertyName("domain")]
    public string? Domain { get; set; }

    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = [];
}

public sealed class ZeroTierMember
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("authorized")]
    public bool Authorized { get; set; }

    [JsonPropertyName("activeBridge")]
    public bool ActiveBridge { get; set; }

    [JsonPropertyName("ipAssignments")]
    public List<string> IpAssignments { get; set; } = [];
}

public sealed class ZeroTierPeer
{
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("latency")]
    public int? Latency { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }
}

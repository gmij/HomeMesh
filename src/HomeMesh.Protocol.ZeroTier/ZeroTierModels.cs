using System;
using System.Text.Json;
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
    [JsonConverter(typeof(ZeroTierDnsConverter))]
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

/// <summary>
/// ZeroTier 各版本对 dns 字段的 JSON 格式不一致（对象/数组/null），此 Converter 统一容错处理。
/// </summary>
internal sealed class ZeroTierDnsConverter : JsonConverter<ZeroTierDns?>
{
    public override ZeroTierDns? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // 部分 ZT 版本返回 array 格式，跳过并返回 null
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Skip();
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            reader.Skip();
            return null;
        }

        var domain = default(string?);
        var servers = new List<string>();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) continue;
            var propName = reader.GetString();
            if (!reader.Read()) break;

            if (string.Equals(propName, "domain", StringComparison.OrdinalIgnoreCase))
            {
                domain = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            }
            else if (string.Equals(propName, "servers", StringComparison.OrdinalIgnoreCase))
            {
                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        if (reader.TokenType == JsonTokenType.String)
                            servers.Add(reader.GetString()!);
                    }
                }
                else if (reader.TokenType != JsonTokenType.Null)
                {
                    reader.Skip();
                }
            }
            else
            {
                reader.Skip();
            }
        }

        return new ZeroTierDns { Domain = domain, Servers = servers };
    }

    public override void Write(Utf8JsonWriter writer, ZeroTierDns? value, JsonSerializerOptions options)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject();
        writer.WriteString("domain", value.Domain);
        writer.WriteStartArray("servers");
        foreach (var s in value.Servers) writer.WriteStringValue(s);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
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

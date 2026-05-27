using System.Buffers;
using System.Text;
using System.Text.Json;

namespace HomeMesh.Application.Members;

internal static class MemberProviderState
{
    private const string DefaultRole = "Device";

    public static string NormalizeRole(string? role)
    {
        var normalized = role?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return DefaultRole;
        }

        return normalized.Equals("unknown", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("unknow", StringComparison.OrdinalIgnoreCase)
            ? DefaultRole
            : normalized;
    }

    public static bool IsManualAuthorizationBlocked(string? providerRawJson)
    {
        return Read(providerRawJson).ManualAuthorizationBlocked;
    }

    public static string? Update(string? existingProviderRawJson, string? providerRawJson = null, bool? manualAuthorizationBlocked = null)
    {
        var current = Read(existingProviderRawJson);
        var effectiveProviderRawJson = providerRawJson ?? current.ProviderRawJson;
        var effectiveManualAuthorizationBlocked = manualAuthorizationBlocked ?? current.ManualAuthorizationBlocked;

        if (!effectiveManualAuthorizationBlocked)
        {
            return effectiveProviderRawJson;
        }

        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);

        writer.WriteStartObject();
        writer.WritePropertyName("homeMesh");
        writer.WriteStartObject();
        writer.WriteBoolean("manualAuthorizationBlocked", true);
        writer.WriteEndObject();

        if (!string.IsNullOrWhiteSpace(effectiveProviderRawJson))
        {
            WriteProviderRaw(writer, effectiveProviderRawJson);
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static MemberProviderStateSnapshot Read(string? providerRawJson)
    {
        if (string.IsNullOrWhiteSpace(providerRawJson))
        {
            return new MemberProviderStateSnapshot(null, false);
        }

        try
        {
            using var document = JsonDocument.Parse(providerRawJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return new MemberProviderStateSnapshot(providerRawJson, false);
            }

            var hasEnvelope = false;
            var manualAuthorizationBlocked = false;
            string? providerRaw = null;

            if (document.RootElement.TryGetProperty("homeMesh", out var homeMeshElement)
                && homeMeshElement.ValueKind == JsonValueKind.Object)
            {
                hasEnvelope = true;
                if (homeMeshElement.TryGetProperty("manualAuthorizationBlocked", out var blockedElement)
                    && blockedElement.ValueKind is JsonValueKind.True or JsonValueKind.False)
                {
                    manualAuthorizationBlocked = blockedElement.GetBoolean();
                }
            }

            if (document.RootElement.TryGetProperty("providerRaw", out var providerRawElement))
            {
                hasEnvelope = true;
                providerRaw = providerRawElement.GetRawText();
            }
            else if (document.RootElement.TryGetProperty("providerRawText", out var providerRawTextElement)
                     && providerRawTextElement.ValueKind == JsonValueKind.String)
            {
                hasEnvelope = true;
                providerRaw = providerRawTextElement.GetString();
            }

            return hasEnvelope
                ? new MemberProviderStateSnapshot(providerRaw, manualAuthorizationBlocked)
                : new MemberProviderStateSnapshot(providerRawJson, false);
        }
        catch (JsonException)
        {
            return new MemberProviderStateSnapshot(providerRawJson, false);
        }
    }

    private static void WriteProviderRaw(Utf8JsonWriter writer, string providerRawJson)
    {
        try
        {
            using var document = JsonDocument.Parse(providerRawJson);
            writer.WritePropertyName("providerRaw");
            document.RootElement.WriteTo(writer);
        }
        catch (JsonException)
        {
            writer.WriteString("providerRawText", providerRawJson);
        }
    }

    private sealed record MemberProviderStateSnapshot(string? ProviderRawJson, bool ManualAuthorizationBlocked);
}

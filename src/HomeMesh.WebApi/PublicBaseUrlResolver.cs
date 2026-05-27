using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

[assembly: InternalsVisibleTo("HomeMesh.Tests")]

namespace HomeMesh.WebApi;

internal static class PublicBaseUrlResolver
{
    private const string PublicBaseUrlConfigKey = "PublicBaseUrl";

    public static string Resolve(HttpRequest request, IConfiguration configuration)
    {
        var configuredValue = configuration[PublicBaseUrlConfigKey]
            ?? Environment.GetEnvironmentVariable("PUBLIC_BASE_URL");

        if (Uri.TryCreate(configuredValue, UriKind.Absolute, out var configuredUri))
        {
            return configuredUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host.Value}";
    }
}

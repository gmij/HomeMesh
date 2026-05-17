namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierOptions
{
    public bool Enabled { get; set; } = true;
    public string ApiBaseUrl { get; set; } = "http://127.0.0.1:9993";
    public string AuthTokenPath { get; set; } = "/var/lib/zerotier-one/authtoken.secret";
}

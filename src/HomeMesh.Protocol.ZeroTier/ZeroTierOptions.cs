namespace HomeMesh.Protocol.ZeroTier;

public sealed class ZeroTierOptions
{
    public bool Enabled { get; set; } = true;

    /// <summary>ZeroTier 本地控制器监听端口（默认 9993）。ZeroTier 始终与控制器同机运行，无需配置远程 URL。</summary>
    public int Port { get; set; } = 9993;

    public string AuthTokenPath { get; set; } = "/var/lib/zerotier-one/authtoken.secret";
}

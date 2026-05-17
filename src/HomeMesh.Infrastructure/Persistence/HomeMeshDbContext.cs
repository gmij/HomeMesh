using HomeMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeMesh.Infrastructure.Persistence;

public sealed class HomeMeshDbContext(DbContextOptions<HomeMeshDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Home> Homes => Set<Home>();
    public DbSet<Network> Networks => Set<Network>();
    public DbSet<NetworkProviderBinding> NetworkProviderBindings => Set<NetworkProviderBinding>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<NetworkMember> NetworkMembers => Set<NetworkMember>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<IpPool> IpPools => Set<IpPool>();
    public DbSet<DnsConfig> DnsConfigs => Set<DnsConfig>();
    public DbSet<Gateway> Gateways => Set<Gateway>();
    public DbSet<EnrollmentToken> EnrollmentTokens => Set<EnrollmentToken>();
    public DbSet<AclPolicy> AclPolicies => Set<AclPolicy>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ProviderSyncState> ProviderSyncStates => Set<ProviderSyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.Property(x => x.Username).HasMaxLength(128);
        });

        modelBuilder.Entity<Home>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<Network>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.HomeId);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Status).HasMaxLength(32);
        });

        modelBuilder.Entity<NetworkProviderBinding>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.NetworkId, x.Provider }).IsUnique();
            entity.HasIndex(x => new { x.Provider, x.ProviderNetworkId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(64);
            entity.Property(x => x.ProviderNetworkId).HasMaxLength(128);
        });

        modelBuilder.Entity<Device>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.HomeId);
            entity.HasIndex(x => x.Fingerprint);
            entity.Property(x => x.Name).HasMaxLength(128);
            entity.Property(x => x.Platform).HasMaxLength(64);
            entity.Property(x => x.TrustLevel).HasMaxLength(32);
        });

        modelBuilder.Entity<NetworkMember>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.NetworkId, x.Provider, x.ProviderMemberId }).IsUnique();
            entity.Property(x => x.Provider).HasMaxLength(64);
            entity.Property(x => x.ProviderMemberId).HasMaxLength(128);
            entity.Property(x => x.Role).HasMaxLength(64);
        });

        modelBuilder.Entity<Route>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NetworkId);
            entity.Property(x => x.Type).HasMaxLength(64);
            entity.Property(x => x.Target).HasMaxLength(128);
            entity.Property(x => x.Via).HasMaxLength(128);
        });

        modelBuilder.Entity<IpPool>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NetworkId);
        });

        modelBuilder.Entity<DnsConfig>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NetworkId).IsUnique();
        });

        modelBuilder.Entity<Gateway>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.NetworkId);
            entity.HasIndex(x => x.MemberId);
        });

        modelBuilder.Entity<EnrollmentToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.HasIndex(x => x.NetworkId);
        });

        modelBuilder.Entity<AclPolicy>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.HomeId);
            entity.HasIndex(x => x.NetworkId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.CreatedAt);
            entity.HasIndex(x => x.Type);
        });

        modelBuilder.Entity<ProviderSyncState>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Provider, x.ResourceType, x.ResourceId });
        });
    }
}

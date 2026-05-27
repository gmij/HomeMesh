using HomeMesh.Abstractions.Providers;
using HomeMesh.Application.Members;
using HomeMesh.Application.Networks;
using HomeMesh.Application.Sync;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HomeMesh.Tests;

public sealed class MemberWorkflowTests
{
    [Fact]
    public async Task ListAsync_Should_Normalize_Unknown_Role_To_Device()
    {
        await using var database = await TestDatabase.CreateAsync();

        database.Db.NetworkMembers.Add(new NetworkMember
        {
            Id = "member-1",
            NetworkId = "network-1",
            Provider = "ZeroTier",
            ProviderMemberId = "abcdef1234",
            Role = "unknow"
        });
        await database.Db.SaveChangesAsync();

        var service = new MemberService(database.Db, []);

        var members = await service.ListAsync("network-1");

        Assert.Single(members);
        Assert.Equal("Device", members[0].Role);
    }

    [Fact]
    public async Task SyncMembersAsync_Should_Not_Reauthorize_Manually_Revoked_Member_When_AutoApprove_Is_Enabled()
    {
        await using var database = await TestDatabase.CreateAsync();
        var provider = new FakeProvider
        {
            Members =
            [
                new VirtualMemberInfo("abcdef1234", "Laptop", false, false, true, Array.Empty<string>())
            ]
        };

        database.Db.Networks.Add(new Network
        {
            Id = "network-1",
            HomeId = "home-1",
            Name = "Home",
            AutoApproveMembers = true
        });
        database.Db.NetworkProviderBindings.Add(new NetworkProviderBinding
        {
            Id = "binding-1",
            NetworkId = "network-1",
            Provider = provider.ProviderName,
            ProviderNetworkId = "zt-network-1",
            IsPrimary = true
        });
        database.Db.NetworkMembers.Add(new NetworkMember
        {
            Id = "member-1",
            NetworkId = "network-1",
            Provider = provider.ProviderName,
            ProviderMemberId = "abcdef1234",
            Authorized = false,
            ProviderRawJson = "{\"homeMesh\":{\"manualAuthorizationBlocked\":true}}"
        });
        await database.Db.SaveChangesAsync();

        var memberService = new MemberService(database.Db, [provider]);
        var syncService = new NetworkSyncService(database.Db, [provider], memberService, NullLogger<NetworkSyncService>.Instance);

        var result = await syncService.SyncMembersAsync("network-1");
        var member = await database.Db.NetworkMembers.SingleAsync(x => x.Id == "member-1");

        Assert.Equal(0, result.AutoApprovedMemberCount);
        Assert.Equal(0, provider.UpdateMemberCallCount);
        Assert.False(member.Authorized);
    }

    [Fact]
    public async Task CreateAsync_Should_Disable_AutoAssign_By_Default_Until_Pool_Is_Configured()
    {
        await using var database = await TestDatabase.CreateAsync();
        var provider = new FakeProvider();
        database.Db.Homes.Add(new Home
        {
            Id = "home-1",
            Name = "My Home"
        });
        await database.Db.SaveChangesAsync();

        var service = new NetworkService(database.Db, [provider]);

        var created = await service.CreateAsync(new CreateNetworkRequest("Home", provider.ProviderName, "10.10.0.0/24"));
        var network = await database.Db.Networks.SingleAsync(x => x.Id == created.Id);

        Assert.False(network.V4AssignMode);
    }

    private sealed class FakeProvider : ISdwanControllerProvider
    {
        public string ProviderName => "ZeroTier";

        public IReadOnlyList<VirtualMemberInfo> Members { get; set; } = Array.Empty<VirtualMemberInfo>();

        public int UpdateMemberCallCount { get; private set; }

        public Task<ProviderHealthStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<VirtualNetworkInfo>> ListNetworksAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<VirtualNetworkInfo> CreateNetworkAsync(CreateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new VirtualNetworkInfo("zt-network-1", request.Name, request.Cidr, request.Private, [], [], null));
        }

        public Task<VirtualNetworkInfo> GetNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<VirtualNetworkInfo> UpdateNetworkAsync(string providerNetworkId, UpdateVirtualNetworkRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteNetworkAsync(string providerNetworkId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<VirtualMemberInfo>> ListMembersAsync(string providerNetworkId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Members);
        }

        public Task<VirtualMemberInfo> UpdateMemberAsync(
            string providerNetworkId,
            string providerMemberId,
            UpdateVirtualMemberRequest request,
            CancellationToken cancellationToken = default)
        {
            UpdateMemberCallCount++;
            return Task.FromResult(new VirtualMemberInfo(
                providerMemberId,
                "Laptop",
                request.Authorized ?? false,
                request.ActiveBridge ?? false,
                true,
                request.IpAssignments ?? Array.Empty<string>()));
        }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, HomeMeshDbContext db)
        {
            _connection = connection;
            Db = db;
        }

        public HomeMeshDbContext Db { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<HomeMeshDbContext>()
                .UseSqlite(connection)
                .Options;

            var db = new HomeMeshDbContext(options);
            await db.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, db);
        }

        public async ValueTask DisposeAsync()
        {
            await Db.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}

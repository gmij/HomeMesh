using HomeMesh.Application.NetworkConfig;
using HomeMesh.Domain.Entities;
using HomeMesh.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HomeMesh.Tests;

public sealed class NetworkConfigWorkflowTests
{
    [Fact]
    public async Task CreateAsync_Should_Reject_Duplicate_IpPool_Range()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Db.Homes.Add(new Home
        {
            Id = "home-1",
            Name = "My Home"
        });
        database.Db.Networks.Add(new Network
        {
            Id = "network-1",
            HomeId = "home-1",
            Name = "Home"
        });
        await database.Db.SaveChangesAsync();

        var service = new IpPoolService(database.Db);

        await service.CreateAsync("network-1", new CreateIpPoolRequest("10.66.100", "10.66.200"));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync("network-1", new CreateIpPoolRequest("10.66.100", "10.66.200")));

        Assert.Equal("IP pool already exists.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_Should_Remove_Existing_IpPool()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Db.Homes.Add(new Home
        {
            Id = "home-1",
            Name = "My Home"
        });
        database.Db.Networks.Add(new Network
        {
            Id = "network-1",
            HomeId = "home-1",
            Name = "Home"
        });
        database.Db.IpPools.Add(new IpPool
        {
            Id = "pool-1",
            NetworkId = "network-1",
            IpRangeStart = "10.66.100",
            IpRangeEnd = "10.66.200",
            ProviderManaged = true
        });
        await database.Db.SaveChangesAsync();

        var service = new IpPoolService(database.Db);

        var deleted = await service.DeleteAsync("network-1", "pool-1");

        Assert.True(deleted);
        Assert.Empty(await database.Db.IpPools.ToListAsync());
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

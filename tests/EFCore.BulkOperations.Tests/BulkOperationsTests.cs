using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EFCore.BulkOperations.Tests;

/// <summary>
/// Tests run against SQLite, which exercises the chunked-SaveChanges fallback path
/// (the SqlBulkCopy path only activates for the SQL Server provider and is validated
/// separately via manual/integration testing against a real SQL Server instance).
/// </summary>
public class BulkOperationsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _context;

    public BulkOperationsTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task BulkInsertAsync_Inserts_All_Entities_Via_Fallback()
    {
        var entities = Enumerable.Range(1, 250)
            .Select(i => new TestEntity { Name = $"Item {i}", Score = i })
            .ToList();

        await _context.BulkInsertAsync(entities, new BulkInsertOptions { BatchSize = 50 });

        Assert.Equal(250, await _context.Entities.CountAsync());
    }

    [Fact]
    public async Task BulkInsertAsync_Respects_Small_Batch_Size()
    {
        var entities = Enumerable.Range(1, 7)
            .Select(i => new TestEntity { Name = $"Item {i}", Score = i })
            .ToList();

        await _context.BulkInsertAsync(entities, new BulkInsertOptions { BatchSize = 3 });

        Assert.Equal(7, await _context.Entities.CountAsync());
        Assert.Equal(28, await _context.Entities.SumAsync(e => e.Score));
    }

    [Fact]
    public async Task BulkInsertAsync_With_Empty_Collection_Inserts_Nothing()
    {
        await _context.BulkInsertAsync(Array.Empty<TestEntity>());

        Assert.Equal(0, await _context.Entities.CountAsync());
    }

    [Fact]
    public async Task BulkUpdateAsync_Updates_Matching_Rows()
    {
        _context.Entities.AddRange(
            new TestEntity { Name = "A", Score = 1 },
            new TestEntity { Name = "B", Score = 2 });
        await _context.SaveChangesAsync();

        var updated = await _context.Entities
            .Where(e => e.Score < 5)
            .BulkUpdateAsync(setters => setters.SetProperty(e => e.Score, e => e.Score + 100));

        Assert.Equal(2, updated);
        Assert.Equal(101, await _context.Entities.Where(e => e.Name == "A").Select(e => e.Score).SingleAsync());
    }

    [Fact]
    public async Task BulkDeleteAsync_Deletes_Matching_Rows()
    {
        _context.Entities.AddRange(
            new TestEntity { Name = "Keep", Score = 1 },
            new TestEntity { Name = "Remove", Score = 99 });
        await _context.SaveChangesAsync();

        var deleted = await _context.Entities.Where(e => e.Score > 50).BulkDeleteAsync();

        Assert.Equal(1, deleted);
        Assert.Equal(1, await _context.Entities.CountAsync());
    }
}

public class BulkPropertySelectorTests
{
    [Fact]
    public void SelectInsertableProperties_Excludes_Identity_Primary_Key()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(TestEntity))!;
        var properties = BulkPropertySelector.SelectInsertableProperties(entityType, excludeDatabaseGenerated: true);

        Assert.DoesNotContain(properties, p => p.Name == nameof(TestEntity.Id));
        Assert.Contains(properties, p => p.Name == nameof(TestEntity.Name));
        Assert.Contains(properties, p => p.Name == nameof(TestEntity.Score));
    }

    [Fact]
    public void SelectInsertableProperties_Includes_Identity_When_Not_Excluded()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
        using var context = new TestDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(TestEntity))!;
        var properties = BulkPropertySelector.SelectInsertableProperties(entityType, excludeDatabaseGenerated: false);

        Assert.Contains(properties, p => p.Name == nameof(TestEntity.Id));
    }
}

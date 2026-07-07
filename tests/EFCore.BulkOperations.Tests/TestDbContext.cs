using Microsoft.EntityFrameworkCore;

namespace EFCore.BulkOperations.Tests;

public class TestEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestEntity> Entities => Set<TestEntity>();
}

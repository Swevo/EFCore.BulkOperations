using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace EFCore.BulkOperations;

/// <summary>
/// Bulk insert/update/delete extensions for <see cref="DbContext"/>.
/// </summary>
public static class BulkOperationsExtensions
{
    private const string SqlServerProviderName = "Microsoft.EntityFrameworkCore.SqlServer";

    /// <summary>
    /// Inserts <paramref name="entities"/> in bulk. On SQL Server this uses <see cref="SqlBulkCopy"/>
    /// for high-throughput inserts; on other providers it falls back to chunked
    /// <c>AddRange</c> + <c>SaveChangesAsync</c> batches so the same API works everywhere.
    /// </summary>
    public static async Task BulkInsertAsync<TEntity>(
        this DbContext context,
        IEnumerable<TEntity> entities,
        BulkInsertOptions? options = null,
        CancellationToken cancellationToken = default)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entities);
        options ??= new BulkInsertOptions();

        if (context.Database.ProviderName == SqlServerProviderName)
        {
            await BulkInsertViaSqlBulkCopyAsync(context, entities, options, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await BulkInsertViaChunkedSaveChangesAsync(context, entities, options, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Convenience wrapper around EF Core's <c>ExecuteUpdateAsync</c>, kept for API symmetry
    /// with <see cref="BulkInsertAsync{TEntity}"/> and <see cref="BulkDeleteAsync{TEntity}"/>.
    /// </summary>
    public static Task<int> BulkUpdateAsync<TEntity>(
        this IQueryable<TEntity> query,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => query.ExecuteUpdateAsync(setPropertyCalls, cancellationToken);

    /// <summary>
    /// Convenience wrapper around EF Core's <c>ExecuteDeleteAsync</c>, kept for API symmetry
    /// with <see cref="BulkInsertAsync{TEntity}"/> and <see cref="BulkUpdateAsync{TEntity}"/>.
    /// </summary>
    public static Task<int> BulkDeleteAsync<TEntity>(
        this IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
        where TEntity : class
        => query.ExecuteDeleteAsync(cancellationToken);

    private static async Task BulkInsertViaSqlBulkCopyAsync<TEntity>(
        DbContext context,
        IEnumerable<TEntity> entities,
        BulkInsertOptions options,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not part of the EF Core model.");

        var properties = BulkPropertySelector.SelectInsertableProperties(entityType, options.ExcludeDatabaseGeneratedColumns);
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} is not mapped to a table.");
        var schema = entityType.GetSchema();
        var qualifiedTableName = schema is null ? $"[{tableName}]" : $"[{schema}].[{tableName}]";

        var connection = (SqlConnection)context.Database.GetDbConnection();
        var wasClosed = connection.State != System.Data.ConnectionState.Open;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var transaction = context.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;

            using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction)
            {
                DestinationTableName = qualifiedTableName,
                BatchSize = options.BatchSize,
                BulkCopyTimeout = options.TimeoutSeconds,
            };

            foreach (var property in properties)
            {
                var columnName = property.GetColumnName() ?? property.Name;
                bulkCopy.ColumnMappings.Add(columnName, columnName);
            }

            using var reader = new EntityDataReader<TEntity>(entities, properties);
            await bulkCopy.WriteToServerAsync(reader, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private static async Task BulkInsertViaChunkedSaveChangesAsync<TEntity>(
        DbContext context,
        IEnumerable<TEntity> entities,
        BulkInsertOptions options,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var batch = new List<TEntity>(options.BatchSize);

        foreach (var entity in entities)
        {
            batch.Add(entity);
            if (batch.Count < options.BatchSize)
            {
                continue;
            }

            await FlushAsync(context, batch, cancellationToken).ConfigureAwait(false);
            batch.Clear();
        }

        if (batch.Count > 0)
        {
            await FlushAsync(context, batch, cancellationToken).ConfigureAwait(false);
        }

        static async Task FlushAsync(DbContext ctx, List<TEntity> items, CancellationToken ct)
        {
            ctx.Set<TEntity>().AddRange(items);
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            ctx.ChangeTracker.Clear();
        }
    }
}

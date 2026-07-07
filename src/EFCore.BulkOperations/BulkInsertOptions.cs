namespace EFCore.BulkOperations;

/// <summary>
/// Options controlling <see cref="BulkOperationsExtensions.BulkInsertAsync{TEntity}"/> behavior.
/// </summary>
public sealed class BulkInsertOptions
{
    /// <summary>Number of rows sent per batch. Applies to both the SqlBulkCopy path and the fallback path.</summary>
    public int BatchSize { get; set; } = 2000;

    /// <summary>Timeout (seconds) for the bulk copy operation on SQL Server. Ignored on the fallback path.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// When true (default), properties whose value is database-generated on insert
    /// (identity columns, computed columns) are excluded from the bulk insert payload.
    /// </summary>
    public bool ExcludeDatabaseGeneratedColumns { get; set; } = true;
}

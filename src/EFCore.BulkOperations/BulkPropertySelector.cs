using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EFCore.BulkOperations;

internal static class BulkPropertySelector
{
    /// <summary>
    /// Selects the scalar, non-navigation properties to include in a bulk insert payload,
    /// optionally excluding database-generated columns (identity/computed) so their values
    /// aren't sent to the server.
    /// </summary>
    public static IReadOnlyList<IProperty> SelectInsertableProperties(IEntityType entityType, bool excludeDatabaseGenerated)
    {
        var properties = new List<IProperty>();

        foreach (var property in entityType.GetProperties())
        {
            if (property.GetColumnName() is null)
            {
                continue;
            }

            if (excludeDatabaseGenerated && IsDatabaseGenerated(property))
            {
                continue;
            }

            properties.Add(property);
        }

        return properties;
    }

    private static bool IsDatabaseGenerated(IProperty property)
    {
        if (property.GetComputedColumnSql() is not null)
        {
            return true;
        }

        // Treat "ValueGenerated.OnAdd" primary keys with no explicit default/value generator
        // supplied by the app as identity columns whose values are assigned by the server.
        if (property.ValueGenerated == Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd && property.IsPrimaryKey())
        {
            return true;
        }

        return false;
    }
}

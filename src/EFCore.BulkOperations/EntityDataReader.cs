using System.Collections;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EFCore.BulkOperations;

/// <summary>
/// Minimal forward-only <see cref="IDataReader"/> over a sequence of entities, exposing only
/// the columns selected for the bulk insert. Used to feed <c>SqlBulkCopy.WriteToServerAsync</c>
/// without materializing an intermediate <see cref="DataTable"/>.
/// </summary>
internal sealed class EntityDataReader<TEntity> : IDataReader
{
    private readonly IEnumerator<TEntity> _enumerator;
    private readonly IReadOnlyList<IProperty> _properties;
    private bool _disposed;

    public EntityDataReader(IEnumerable<TEntity> entities, IReadOnlyList<IProperty> properties)
    {
        _enumerator = entities.GetEnumerator();
        _properties = properties;
    }

    public int FieldCount => _properties.Count;

    public string GetName(int i) => _properties[i].GetColumnName() ?? _properties[i].Name;

    public object GetValue(int i)
    {
        var current = _enumerator.Current;
        var value = _properties[i].GetGetter().GetClrValue(current!);
        return value ?? DBNull.Value;
    }

    public bool Read() => _enumerator.MoveNext();

    public int GetOrdinal(string name)
    {
        for (var i = 0; i < _properties.Count; i++)
        {
            if (string.Equals(GetName(i), name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException(name);
    }

    public bool IsDBNull(int i) => GetValue(i) is DBNull;

    // --- Members required by IDataReader/IDataRecord but unused by SqlBulkCopy ---
    public void Dispose()
    {
        if (!_disposed)
        {
            _enumerator.Dispose();
            _disposed = true;
        }
    }

    public void Close() => Dispose();
    public bool NextResult() => false;
    public int Depth => 0;
    public bool IsClosed => _disposed;
    public int RecordsAffected => -1;
    public DataTable? GetSchemaTable() => null;

    public string GetDataTypeName(int i) => GetValue(i)?.GetType().Name ?? "Object";
    public Type GetFieldType(int i) => _properties[i].ClrType;
    public object this[int i] => GetValue(i);
    public object this[string name] => GetValue(GetOrdinal(name));

    public bool GetBoolean(int i) => Convert.ToBoolean(GetValue(i));
    public byte GetByte(int i) => Convert.ToByte(GetValue(i));
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public char GetChar(int i) => Convert.ToChar(GetValue(i));
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public IDataReader GetData(int i) => throw new NotSupportedException();
    public DateTime GetDateTime(int i) => Convert.ToDateTime(GetValue(i));
    public decimal GetDecimal(int i) => Convert.ToDecimal(GetValue(i));
    public double GetDouble(int i) => Convert.ToDouble(GetValue(i));
    public float GetFloat(int i) => Convert.ToSingle(GetValue(i));
    public Guid GetGuid(int i) => (Guid)GetValue(i);
    public short GetInt16(int i) => Convert.ToInt16(GetValue(i));
    public int GetInt32(int i) => Convert.ToInt32(GetValue(i));
    public long GetInt64(int i) => Convert.ToInt64(GetValue(i));
    public string GetString(int i) => Convert.ToString(GetValue(i)) ?? string.Empty;
    public int GetValues(object[] values)
    {
        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }

        return count;
    }
}

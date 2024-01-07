using System.Data;
using System.Diagnostics;
using FluentAssertions.DataSets;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace FluentAssertions;

/// <summary>
/// Contains an extension method for custom assertions in unit tests related to DataTable objects.
/// </summary>
[DebuggerNonUserCode]
public static class DataTableAssertionExtensions
{
    /// <summary>
    /// Returns a <see cref="DataTableAssertions{TDataTable}"/> object that can be used to assert the
    /// current <see cref="DataTable"/>.
    /// </summary>
    [Pure]
    public static DataTableAssertions<TDataTable> Should<TDataTable>(this TDataTable actualValue)
        where TDataTable : DataTable
    {
        return new DataTableAssertions<TDataTable>(actualValue);
    }
}

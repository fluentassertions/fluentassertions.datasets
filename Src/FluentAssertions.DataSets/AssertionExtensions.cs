using System.Data;
using System.Diagnostics;
using FluentAssertions.Collections;
using FluentAssertions.DataSets;
using FluentAssertions.DataSets.Common;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace FluentAssertions;

/// <summary>
/// Contains extension methods for custom assertions in unit tests.
/// </summary>
[DebuggerNonUserCode]
public static class AssertionExtensions
{
    /// <summary>
    /// Returns an assertions object that provides methods for asserting the state of a <see cref="DataTableCollection"/>.
    /// </summary>
    [Pure]
    public static GenericCollectionAssertions<DataTable> Should(this DataTableCollection actualValue)
    {
        return new GenericCollectionAssertions<DataTable>(
            ReadOnlyNonGenericCollectionWrapper.Create(actualValue));
    }

    /// <summary>
    /// Returns an assertions object that provides methods for asserting the state of a <see cref="DataColumnCollection"/>.
    /// </summary>
    [Pure]
    public static GenericCollectionAssertions<DataColumn> Should(this DataColumnCollection actualValue)
    {
        return new GenericCollectionAssertions<DataColumn>(
            ReadOnlyNonGenericCollectionWrapper.Create(actualValue));
    }

    /// <summary>
    /// Returns an assertions object that provides methods for asserting the state of a <see cref="DataRowCollection"/>.
    /// </summary>
    [Pure]
    public static GenericCollectionAssertions<DataRow> Should(this DataRowCollection actualValue)
    {
        return new GenericCollectionAssertions<DataRow>(
            ReadOnlyNonGenericCollectionWrapper.Create(actualValue));
    }

    /// <summary>
    /// Returns a <see cref="DataColumnAssertions"/> object that can be used to assert the
    /// current <see cref="DataColumn"/>.
    /// </summary>
    [Pure]
    public static DataColumnAssertions Should(this DataColumn actualValue)
    {
        return new DataColumnAssertions(actualValue);
    }
}

﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;

namespace FluentAssertions.DataSets;

/// <summary>
/// Provides convenient assertion methods on a <see cref="DataSet"/> that can be
/// used to assert equivalency and the presence of tables.
/// </summary>
[DebuggerNonUserCode]
public class DataSetAssertions<TDataSet> : ReferenceTypeAssertions<DataSet, DataSetAssertions<TDataSet>>
    where TDataSet : DataSet
{
    public DataSetAssertions(TDataSet dataSet, AssertionChain assertionChain)
        : base(dataSet, assertionChain)
    {
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> contains exactly the expected number of tables in its <see cref="DataSet.Tables"/> collection.
    /// </summary>
    /// <param name="expected">The expected number of rows.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    public AndConstraint<DataSetAssertions<TDataSet>> HaveTableCount(int expected, string because = "",
        params object[] becauseArgs)
    {
        CurrentAssertionChain
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:DataSet} to contain exactly {0} table(s){reason}, but found <null>.", expected);

        if (CurrentAssertionChain.Succeeded)
        {
            int actualCount = Subject.Tables.Count;

            CurrentAssertionChain
                .ForCondition(actualCount == expected)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:DataSet} to contain exactly {0} table(s){reason}, but found {1}.", expected,
                    actualCount);
        }

        return new AndConstraint<DataSetAssertions<TDataSet>>(this);
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> contains a table with the expected name.
    /// </summary>
    /// <param name="expectedTableName">The value that is expected in <see cref="DataTable.TableName"/>.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    public AndWhichConstraint<DataSetAssertions<TDataSet>, DataTable> HaveTable(string expectedTableName, string because = "",
        params object[] becauseArgs)
    {
        var subjectTable = default(DataTable);

        if (Subject is null)
        {
            CurrentAssertionChain
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:DataSet} to contain a table named {0}{reason}, but found <null>.",
                    expectedTableName);
        }
        else if (!Subject.Tables.Contains(expectedTableName))
        {
            CurrentAssertionChain
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:DataSet} to contain a table named {0}{reason}, but it does not.", expectedTableName);
        }
        else
        {
            subjectTable = Subject.Tables[expectedTableName];
        }

        return new AndWhichConstraint<DataSetAssertions<TDataSet>, DataTable>(this, subjectTable);
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> has tables with all of the supplied expected column names.
    /// </summary>
    /// <param name="expectedTableNames">An array of values expected in <see cref="DataTable.TableName"/>.</param>
    public AndConstraint<DataSetAssertions<TDataSet>> HaveTables(params string[] expectedTableNames)
    {
        return HaveTables((IEnumerable<string>)expectedTableNames);
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> has tables with all of the supplied expected table names.
    /// </summary>
    /// <param name="expectedTableNames">An <see cref="IEnumerable{T}"/> of string values expected in <see cref="DataTable.TableName"/>.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    public AndConstraint<DataSetAssertions<TDataSet>> HaveTables(IEnumerable<string> expectedTableNames, string because = "",
        params object[] becauseArgs)
    {
        CurrentAssertionChain
            .ForCondition(Subject is not null)
            .BecauseOf(because, becauseArgs)
            .FailWith("Expected {context:DataSet} to contain {0} table(s) with specific names{reason}, but found <null>.",
                () => expectedTableNames.Count());

        if (CurrentAssertionChain.Succeeded)
        {
            foreach (var expectedTableName in expectedTableNames)
            {
                CurrentAssertionChain
                    .ForCondition(Subject.Tables.Contains(expectedTableName))
                    .BecauseOf(because, becauseArgs)
                    .FailWith("Expected {context:DataSet} to contain a table named {0}{reason}, but it does not.", expectedTableName);
            }
        }

        return new AndConstraint<DataSetAssertions<TDataSet>>(this);
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> is equivalent to another.
    /// </summary>
    /// <remarks>
    /// Data sets are equivalent when their <see cref="DataSet.Tables"/> and <see cref="DataSet.ExtendedProperties"/>
    /// collections are equivalent and the following members have the same values:
    ///
    /// <list type="bullet">
    ///   <item><description>DataSetName</description></item>
    ///   <item><description>CaseSensitive</description></item>
    ///   <item><description>EnforceConstraints</description></item>
    ///   <item><description>HasErrors</description></item>
    ///   <item><description>Locale</description></item>
    ///   <item><description>Namespace</description></item>
    ///   <item><description>Prefix</description></item>
    ///   <item><description>RemotingFormat</description></item>
    ///   <item><description>SchemaSerializationMode</description></item>
    /// </list>
    ///
    /// The <see cref="DataSet"/> objects must be of the same type; if two <see cref="DataSet"/> objects
    /// are equivalent in all ways, except that one is a custom subclass of <see cref="DataSet"/> (e.g. to provide
    /// typed accessors for <see cref="DataTable"/> values contained by the <see cref="DataSet"/>), then by default,
    /// they will not be considered equivalent. This can be overridden by using the overload that takes
    /// <see cref="IDataEquivalencyAssertionOptions{DataSet}"/>.
    /// </remarks>
    /// <param name="expectation">A <see cref="DataColumn"/> with the expected configuration.</param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    public AndConstraint<DataSetAssertions<TDataSet>> BeEquivalentTo(DataSet expectation, string because = "",
        params object[] becauseArgs)
    {
        return BeEquivalentTo(
            expectation,
            options => options,
            because,
            becauseArgs);
    }

    /// <summary>
    /// Asserts that an instance of <see cref="DataSet"/> is equivalent to another.
    /// </summary>
    /// <remarks>
    /// Data sets are equivalent when their <see cref="DataSet.Tables"/> and <see cref="DataSet.ExtendedProperties"/>
    /// collections are equivalent and the following members have the same values:
    ///
    /// <list type="bullet">
    ///   <item><description>DataSetName</description></item>
    ///   <item><description>CaseSensitive</description></item>
    ///   <item><description>EnforceConstraints</description></item>
    ///   <item><description>HasErrors</description></item>
    ///   <item><description>Locale</description></item>
    ///   <item><description>Namespace</description></item>
    ///   <item><description>Prefix</description></item>
    ///   <item><description>RemotingFormat</description></item>
    ///   <item><description>SchemaSerializationMode</description></item>
    /// </list>
    ///
    /// <para>
    /// The <see cref="DataSet"/> objects must be of the same type; if two <see cref="DataSet"/> objects
    /// are equivalent in all ways, except that one is a custom subclass of <see cref="DataSet"/> (e.g. to provide
    /// typed accessors for <see cref="DataTable"/> values contained by the <see cref="DataSet"/>), then by default,
    /// they will not be considered equivalent.
    /// </para>
    /// <para>
    /// This, as well as testing of any property can be overridden using the <paramref name="config"/> callback.
    /// By calling <see cref="IDataEquivalencyAssertionOptions{T}.AllowingMismatchedTypes"/>, two <see cref="DataSet"/>
    /// objects of differing types can be considered equivalent. This setting applies to all types recursively tested
    /// as part of the <see cref="DataSet"/>.
    /// </para>
    /// <para>
    /// Exclude specific properties using <see cref="IDataEquivalencyAssertionOptions{T}.Excluding(System.Linq.Expressions.Expression{Func{T, object}})"/>.
    /// Exclude specific tables within the data set using <see cref="IDataEquivalencyAssertionOptions{T}.ExcludingTable(string)"/>
    /// or a related function. You can also indicate that columns should be excluded within the <see cref="DataTable"/>
    /// objects recursively tested as part of the <see cref="DataSet"/> using <see cref="IDataEquivalencyAssertionOptions{T}.ExcludingColumn(DataColumn)"/>
    /// or a related function. The <see cref="IDataEquivalencyAssertionOptions{T}.ExcludingColumnInAllTables(string)"/> method
    /// can be used to exclude columns across all <see cref="DataTable"/> objects in the <see cref="DataSet"/> that share
    /// the same name.
    /// </para>
    /// <para>
    /// You can use <see cref="IDataEquivalencyAssertionOptions{T}.ExcludingRelated(System.Linq.Expressions.Expression{Func{DataTable, object}})"/>
    /// and related functions to exclude properties on other related System.Data types.
    /// </para>
    /// </remarks>
    /// <param name="expectation">A <see cref="DataColumn"/> with the expected configuration.</param>
    /// <param name="config">
    /// A reference to the <see cref="IDataEquivalencyAssertionOptions{DataSet}"/> configuration object that can be used
    /// to influence the way the object graphs are compared. You can also provide an alternative instance of the
    /// <see cref="IDataEquivalencyAssertionOptions{DataSet}"/> class. The global defaults are determined by the
    /// <see cref="AssertionConfiguration"/> class.
    /// </param>
    /// <param name="because">
    /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
    /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
    /// </param>
    /// <param name="becauseArgs">
    /// Zero or more objects to format using the placeholders in <paramref name="because"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
    public AndConstraint<DataSetAssertions<TDataSet>> BeEquivalentTo(DataSet expectation,
        Func<IDataEquivalencyAssertionOptions<DataSet>, IDataEquivalencyAssertionOptions<DataSet>> config, string because = "",
        params object[] becauseArgs)
    {
        Guard.ThrowIfArgumentIsNull(config);

        var defaults = new DataEquivalencyAssertionOptions<DataSet>(AssertionConfiguration.Current.Equivalency.CloneDefaults<DataSet>());
        config(defaults);

        ((object)Subject).Should().BeEquivalentTo(expectation, _ => defaults, because, becauseArgs);

        return new AndConstraint<DataSetAssertions<TDataSet>>(this);
    }

    protected override string Identifier => "DataSet";
}

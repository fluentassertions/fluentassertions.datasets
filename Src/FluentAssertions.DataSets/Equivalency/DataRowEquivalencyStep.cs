using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class DataRowEquivalencyStep : EquivalencyStep<DataRow>
{
    [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "The code is easier to read without it.")]
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);
        var subject = comparands.Subject as DataRow;

        if (comparands.Expectation is not DataRow expectation)
        {
            if (subject is not null)
            {
                assertionChain.FailWith("Expected {context:DataRow} value to be null, but found {0}", subject);
            }
        }
        else if (subject is null)
        {
            if (comparands.Subject is null)
            {
                assertionChain.FailWith("Expected {context:DataRow} to be non-null, but found null");
            }
            else
            {
                assertionChain.FailWith("Expected {context:DataRow} to be of type {0}, but found {1} instead",
                    expectation.GetType(), comparands.Subject.GetType());
            }
        }
        else
        {
            var dataSetConfig = context.Options as DataEquivalencyAssertionOptions<DataSet>;
            var dataTableConfig = context.Options as DataEquivalencyAssertionOptions<DataTable>;
            var dataRowConfig = context.Options as DataEquivalencyAssertionOptions<DataRow>;

            if (dataSetConfig?.AllowMismatchedTypes != true
                && dataTableConfig?.AllowMismatchedTypes != true
                && dataRowConfig?.AllowMismatchedTypes != true)
            {
                assertionChain
                    .ForCondition(subject.GetType() == expectation.GetType())
                    .FailWith("Expected {context:DataRow} to be of type {0}{reason}, but found {1}",
                        expectation.GetType(), subject.GetType());
            }

            SelectedDataRowMembers selectedMembers =
                GetMembersFromExpectation(comparands, context.CurrentNode, context.Options);

            CompareScalarProperties(subject, expectation, selectedMembers, assertionChain);

            CompareFieldValues(context, nestedValidator, subject, expectation, dataSetConfig, dataTableConfig,
                dataRowConfig, assertionChain);
        }

        return EquivalencyResult.EquivalencyProven;
    }

    private static void CompareScalarProperties(DataRow subject, DataRow expectation, SelectedDataRowMembers selectedMembers, AssertionChain assertionChain)
    {
        // Note: The members here are listed in the XML documentation for the DataRow.BeEquivalentTo extension
        // method in DataRowAssertions.cs. If this ever needs to change, keep them in sync.
        if (selectedMembers.HasErrors)
        {
            assertionChain
                .ForCondition(subject.HasErrors == expectation.HasErrors)
                .FailWith("Expected {context:DataRow} to have HasErrors value of {0}{reason}, but found {1} instead",
                    expectation.HasErrors, subject.HasErrors);
        }

        if (selectedMembers.RowState)
        {
            assertionChain
                .ForCondition(subject.RowState == expectation.RowState)
                .FailWith("Expected {context:DataRow} to have RowState value of {0}{reason}, but found {1} instead",
                    expectation.RowState, subject.RowState);
        }
    }

    [SuppressMessage("Maintainability", "AV1561:Signature contains too many parameters", Justification = "Needs to be refactored")]
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "Needs to be refactored")]
    private static void CompareFieldValues(IEquivalencyValidationContext context, IValidateChildNodeEquivalency parent,
        DataRow subject, DataRow expectation, DataEquivalencyAssertionOptions<DataSet> dataSetConfig,
        DataEquivalencyAssertionOptions<DataTable> dataTableConfig, DataEquivalencyAssertionOptions<DataRow> dataRowConfig,
        AssertionChain assertionChain)
    {
        IEnumerable<string> expectationColumnNames = expectation.Table.Columns.Cast<DataColumn>()
            .Select(col => col.ColumnName);

        IEnumerable<string> subjectColumnNames = subject.Table.Columns.Cast<DataColumn>()
            .Select(col => col.ColumnName);

        bool ignoreUnmatchedColumns =
            dataSetConfig?.IgnoreUnmatchedColumns == true ||
            dataTableConfig?.IgnoreUnmatchedColumns == true ||
            dataRowConfig?.IgnoreUnmatchedColumns == true;

        DataRowVersion subjectVersion =
            subject.RowState == DataRowState.Deleted
                ? DataRowVersion.Original
                : DataRowVersion.Current;

        DataRowVersion expectationVersion =
            expectation.RowState == DataRowState.Deleted
                ? DataRowVersion.Original
                : DataRowVersion.Current;

        bool compareOriginalVersions =
            subject.RowState == DataRowState.Modified && expectation.RowState == DataRowState.Modified;

        if (dataSetConfig?.ExcludeOriginalData == true
            || dataTableConfig?.ExcludeOriginalData == true
            || dataRowConfig?.ExcludeOriginalData == true)
        {
            compareOriginalVersions = false;
        }

        foreach (var columnName in expectationColumnNames.Union(subjectColumnNames))
        {
            DataColumn expectationColumn = expectation.Table.Columns[columnName];
            DataColumn subjectColumn = subject.Table.Columns[columnName];

            if (subjectColumn is not null
                && (dataSetConfig?.ShouldExcludeColumn(subjectColumn) == true
                    || dataTableConfig?.ShouldExcludeColumn(subjectColumn) == true
                    || dataRowConfig?.ShouldExcludeColumn(subjectColumn) == true))
            {
                continue;
            }

            if (!ignoreUnmatchedColumns)
            {
                assertionChain
                    .ForCondition(subjectColumn is not null)
                    .FailWith("Expected {context:DataRow} to have column {0}{reason}, but found none", columnName);

                assertionChain
                    .ForCondition(expectationColumn is not null)
                    .FailWith("Found unexpected column {0} in {context:DataRow}", columnName);
            }

            if (subjectColumn is not null && expectationColumn is not null)
            {
                CompareFieldValue(context, parent, subject, expectation, subjectColumn, subjectVersion, expectationColumn,
                    expectationVersion);

                if (compareOriginalVersions)
                {
                    CompareFieldValue(context, parent, subject, expectation, subjectColumn, DataRowVersion.Original,
                        expectationColumn, DataRowVersion.Original);
                }
            }
        }
    }

    private static void CompareFieldValue(IEquivalencyValidationContext context, IValidateChildNodeEquivalency parent, DataRow subject,
        DataRow expectation, DataColumn subjectColumn, DataRowVersion subjectVersion, DataColumn expectationColumn,
        DataRowVersion expectationVersion)
    {
        IEquivalencyValidationContext nestedContext = context.AsCollectionItem<object>(
            subjectVersion == DataRowVersion.Current
                ? subjectColumn.ColumnName
                : $"{subjectColumn.ColumnName}, DataRowVersion.Original");

        if (nestedContext is not null)
        {
            parent.AssertEquivalencyOf(
                new Comparands(subject[subjectColumn, subjectVersion], expectation[expectationColumn, expectationVersion],
                    typeof(object)),
                nestedContext);
        }
    }

    private sealed class SelectedDataRowMembers
    {
        public bool HasErrors { get; init; }

        public bool RowState { get; init; }
    }

    private static readonly ConcurrentDictionary<(Type CompileTimeType, Type RuntimeType, IEquivalencyOptions Config),
        SelectedDataRowMembers> SelectedMembersCache = new();

    private static SelectedDataRowMembers GetMembersFromExpectation(Comparands comparands, INode currentNode,
        IEquivalencyOptions config)
    {
        var cacheKey = (comparands.CompileTimeType, comparands.RuntimeType, config);

        if (!SelectedMembersCache.TryGetValue(cacheKey, out SelectedDataRowMembers selectedDataRowMembers))
        {
            IEnumerable<IMember> members = Enumerable.Empty<IMember>();

            foreach (IMemberSelectionRule rule in config.SelectionRules)
            {
                members = rule.SelectMembers(currentNode, members,
                    new MemberSelectionContext(comparands.CompileTimeType, comparands.RuntimeType, config));
            }

            IMember[] selectedMembers = members.ToArray();

            selectedDataRowMembers = new SelectedDataRowMembers
            {
                HasErrors = selectedMembers.Any(m => m.Name == nameof(DataRow.HasErrors)),
                RowState = selectedMembers.Any(m => m.Name == nameof(DataRow.RowState))
            };

            SelectedMembersCache.TryAdd(cacheKey, selectedDataRowMembers);
        }

        return selectedDataRowMembers;
    }
}

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class DataRelationEquivalencyStep : EquivalencyStep<DataRelation>
{
    [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "The code is easier to read without it.")]
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);
        var subject = comparands.Subject as DataRelation;

        if (comparands.Expectation is not DataRelation expectation)
        {
            if (subject is not null)
            {
                assertionChain.FailWith("Expected {context:DataRelation} to be null, but found {0}", subject);
            }
        }
        else if (subject is null)
        {
            if (comparands.Subject is null)
            {
                assertionChain.FailWith("Expected {context:DataRelation} value to be non-null, but found null");
            }
            else
            {
                assertionChain.FailWith("Expected {context:DataRelation} of type {0}, but found {1} instead",
                    expectation.GetType(), comparands.Subject.GetType());
            }
        }
        else
        {
            var selectedMembers = GetMembersFromExpectation(context.CurrentNode, comparands, context.Options)
                .ToDictionary(member => member.Name);

            CompareScalarProperties(subject, expectation, selectedMembers, assertionChain);

            CompareCollections(context, comparands, nestedValidator, context.Options, selectedMembers, assertionChain);

            CompareRelationConstraints(context, nestedValidator, subject, expectation, selectedMembers, assertionChain);
        }

        return EquivalencyResult.EquivalencyProven;
    }

    private static void CompareScalarProperties(DataRelation subject, DataRelation expectation,
        Dictionary<string, IMember> selectedMembers, AssertionChain assertionChain)
    {
        if (selectedMembers.ContainsKey(nameof(expectation.RelationName)))
        {
            assertionChain
                .ForCondition(subject.RelationName == expectation.RelationName)
                .FailWith("Expected {context:DataRelation} to have RelationName of {0}{reason}, but found {1}",
                    expectation.RelationName, subject.RelationName);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.Nested)))
        {
            assertionChain
                .ForCondition(subject.Nested == expectation.Nested)
                .FailWith("Expected {context:DataRelation} to have Nested value of {0}{reason}, but found {1}",
                    expectation.Nested, subject.Nested);
        }

        // Special case: Compare name only
        if (selectedMembers.ContainsKey(nameof(expectation.DataSet)))
        {
            assertionChain
                .ForCondition(subject.DataSet?.DataSetName == expectation.DataSet?.DataSetName)
                .FailWith("Expected containing DataSet of {context:DataRelation} to be {0}{reason}, but found {1}",
                    expectation.DataSet?.DataSetName ?? "<null>",
                    subject.DataSet?.DataSetName ?? "<null>");
        }
    }

    private static void CompareCollections(IEquivalencyValidationContext context, Comparands comparands,
        IValidateChildNodeEquivalency parent,
        IEquivalencyOptions config, Dictionary<string, IMember> selectedMembers, AssertionChain assertionChain)
    {
        if (selectedMembers.TryGetValue(nameof(DataRelation.ExtendedProperties), out IMember expectationMember))
        {
            IMember matchingMember = FindMatchFor(expectationMember, context.CurrentNode, comparands.Subject, config, assertionChain);

            if (matchingMember is not null)
            {
                var nestedComparands = new Comparands
                {
                    Subject = matchingMember.GetValue(comparands.Subject),
                    Expectation = expectationMember.GetValue(comparands.Expectation),
                    CompileTimeType = expectationMember.Type
                };

                parent.AssertEquivalencyOf(nestedComparands, context.AsNestedMember(expectationMember));
            }
        }
    }

    private static void CompareRelationConstraints(IEquivalencyValidationContext context, IValidateChildNodeEquivalency parent,
        DataRelation subject, DataRelation expectation,
        Dictionary<string, IMember> selectedMembers,
        AssertionChain assertionChain)
    {
        CompareDataRelationConstraints(
            parent, context, subject, expectation, selectedMembers,
            "Child",
            selectedMembers.ContainsKey(nameof(DataRelation.ChildTable)),
            selectedMembers.ContainsKey(nameof(DataRelation.ChildColumns)),
            selectedMembers.ContainsKey(nameof(DataRelation.ChildKeyConstraint)),
            r => r.ChildColumns,
            r => r.ChildTable, assertionChain);

        CompareDataRelationConstraints(
            parent, context, subject, expectation, selectedMembers,
            "Parent",
            selectedMembers.ContainsKey(nameof(DataRelation.ParentTable)),
            selectedMembers.ContainsKey(nameof(DataRelation.ParentColumns)),
            selectedMembers.ContainsKey(nameof(DataRelation.ParentKeyConstraint)),
            r => r.ParentColumns,
            r => r.ParentTable, assertionChain);
    }

    private static void CompareDataRelationConstraints(
        IValidateChildNodeEquivalency parent, IEquivalencyValidationContext context,
        DataRelation subject, DataRelation expectation, Dictionary<string, IMember> selectedMembers,
        string relationDirection,
        bool compareTable, bool compareColumns, bool compareKeyConstraint,
        Func<DataRelation, DataColumn[]> getColumns,
        Func<DataRelation, DataTable> getOtherTable,
        AssertionChain assertionChain)
    {
        if (compareColumns)
        {
            CompareDataRelationColumns(subject, expectation, getColumns, assertionChain);
        }

        if (compareTable)
        {
            CompareDataRelationTable(subject, expectation, getOtherTable, assertionChain);
        }

        if (compareKeyConstraint)
        {
            CompareDataRelationKeyConstraint(subject, expectation, parent, context, selectedMembers, relationDirection, assertionChain);
        }
    }

    private static void CompareDataRelationColumns(DataRelation subject, DataRelation expectation,
        Func<DataRelation, DataColumn[]> getColumns, AssertionChain assertionChain)
    {
        DataColumn[] subjectColumns = getColumns(subject);
        DataColumn[] expectationColumns = getColumns(expectation);

        // These column references are in different tables in different data sets that _should_ be equivalent
        // to one another.
        assertionChain
            .ForCondition(subjectColumns.Length == expectationColumns.Length)
            .FailWith("Expected {context:DataRelation} to reference {0} column(s){reason}, but found {subjectColumns.Length}",
                expectationColumns.Length, subjectColumns.Length);

        if (assertionChain.Succeeded)
        {
            for (int i = 0; i < expectationColumns.Length; i++)
            {
                DataColumn subjectColumn = subjectColumns[i];
                DataColumn expectationColumn = expectationColumns[i];

                bool columnsAreEquivalent =
                    subjectColumn.Table.TableName == expectationColumn.Table.TableName &&
                    subjectColumn.ColumnName == expectationColumn.ColumnName;

                assertionChain
                    .ForCondition(columnsAreEquivalent)
                    .FailWith(
                        "Expected {context:DataRelation} to reference column {0} in table {1}{reason}, but found a reference to {2} in table {3} instead",
                        expectationColumn.ColumnName,
                        expectationColumn.Table.TableName,
                        subjectColumn.ColumnName,
                        subjectColumn.Table.TableName);
            }
        }
    }

    private static void CompareDataRelationTable(DataRelation subject, DataRelation expectation,
        Func<DataRelation, DataTable> getOtherTable, AssertionChain assertionChain)
    {
        DataTable subjectTable = getOtherTable(subject);
        DataTable expectationTable = getOtherTable(expectation);

        assertionChain
            .ForCondition(subjectTable.TableName == expectationTable.TableName)
            .FailWith("Expected {context:DataRelation} to reference a table named {0}{reason}, but found {1} instead",
                expectationTable.TableName, subjectTable.TableName);
    }

    private static void CompareDataRelationKeyConstraint(DataRelation subject, DataRelation expectation,
        IValidateChildNodeEquivalency parent, IEquivalencyValidationContext context, Dictionary<string, IMember> selectedMembers,
        string relationDirection, AssertionChain assertionChain)
    {
        if (selectedMembers.TryGetValue(relationDirection + "KeyConstraint", out IMember expectationMember))
        {
            IMember subjectMember = FindMatchFor(expectationMember, context.CurrentNode, subject, context.Options, assertionChain);

            var newComparands = new Comparands
            {
                Subject = subjectMember.GetValue(subject),
                Expectation = expectationMember.GetValue(expectation),
                CompileTimeType = expectationMember.Type
            };

            parent.AssertEquivalencyOf(newComparands, context.AsNestedMember(expectationMember));
        }
    }

    private static IMember FindMatchFor(IMember selectedMemberInfo, INode currentNode, object subject,
        IEquivalencyOptions config, AssertionChain assertionChain)
    {
        IEnumerable<IMember> query =
            from rule in config.MatchingRules
            let match = rule.Match(selectedMemberInfo, subject, currentNode, config, assertionChain)
            where match is not null
            select match;

        return query.FirstOrDefault();
    }

    private static IEnumerable<IMember> GetMembersFromExpectation(INode currentNode, Comparands comparands,
        IEquivalencyOptions config)
    {
        IEnumerable<IMember> members = Enumerable.Empty<IMember>();

        foreach (IMemberSelectionRule rule in config.SelectionRules)
        {
            members = rule.SelectMembers(currentNode, members,
                new MemberSelectionContext(comparands.CompileTimeType, comparands.RuntimeType, config));
        }

        return members;
    }
}

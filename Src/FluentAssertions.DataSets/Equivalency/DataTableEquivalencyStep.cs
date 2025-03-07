﻿using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class DataTableEquivalencyStep : EquivalencyStep<DataTable>
{
    [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "The code is easier to read without it.")]
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);

        var subject = comparands.Subject as DataTable;

        if (comparands.Expectation is not DataTable expectation)
        {
            if (subject is not null)
            {
                assertionChain.FailWith("Expected {context:DataTable} value to be null, but found {0}", subject);
            }
        }
        else if (subject is null)
        {
            if (comparands.Subject is null)
            {
                assertionChain.FailWith("Expected {context:DataTable} to be non-null, but found null");
            }
            else
            {
                assertionChain.FailWith("Expected {context:DataTable} to be of type {0}, but found {1} instead",
                    expectation.GetType(), comparands.Subject.GetType());
            }
        }
        else
        {
            var dataSetConfig = context.Options as DataEquivalencyAssertionOptions<DataSet>;
            var dataTableConfig = context.Options as DataEquivalencyAssertionOptions<DataTable>;

            if (dataSetConfig?.AllowMismatchedTypes != true
                && dataTableConfig?.AllowMismatchedTypes != true)
            {
                assertionChain
                    .ForCondition(subject.GetType() == expectation.GetType())
                    .FailWith("Expected {context:DataTable} to be of type {0}{reason}, but found {1}", expectation.GetType(),
                        subject.GetType());
            }

            var selectedMembers = GetMembersFromExpectation(context.CurrentNode, comparands, context.Options)
                .ToDictionary(member => member.Expectation.Name);

            CompareScalarProperties(subject, expectation, selectedMembers, assertionChain);

            CompareCollections(comparands, context, nestedValidator, context.Options, selectedMembers, assertionChain);
        }

        return EquivalencyResult.EquivalencyProven;
    }

    [SuppressMessage("Design", "MA0051:Method is too long")]
    private static void CompareScalarProperties(DataTable subject, DataTable expectation,
        Dictionary<string, IMember> selectedMembers, AssertionChain assertionChain)
    {
        // Note: The members here are listed in the XML documentation for the DataTable.BeEquivalentTo extension
        // method in DataTableAssertions.cs. If this ever needs to change, keep them in sync.
        if (selectedMembers.ContainsKey(nameof(expectation.TableName)))
        {
            assertionChain
                .ForCondition(subject.TableName == expectation.TableName)
                .FailWith("Expected {context:DataTable} to have TableName {0}{reason}, but found {1} instead",
                    expectation.TableName, subject.TableName);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.CaseSensitive)))
        {
            assertionChain
                .ForCondition(subject.CaseSensitive == expectation.CaseSensitive)
                .FailWith("Expected {context:DataTable} to have CaseSensitive value of {0}{reason}, but found {1} instead",
                    expectation.CaseSensitive, subject.CaseSensitive);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.DisplayExpression)))
        {
            assertionChain
                .ForCondition(subject.DisplayExpression == expectation.DisplayExpression)
                .FailWith("Expected {context:DataTable} to have DisplayExpression value of {0}{reason}, but found {1} instead",
                    expectation.DisplayExpression, subject.DisplayExpression);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.HasErrors)))
        {
            assertionChain
                .ForCondition(subject.HasErrors == expectation.HasErrors)
                .FailWith("Expected {context:DataTable} to have HasErrors value of {0}{reason}, but found {1} instead",
                    expectation.HasErrors, subject.HasErrors);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.Locale)))
        {
            assertionChain
                .ForCondition(Equals(subject.Locale, expectation.Locale))
                .FailWith("Expected {context:DataTable} to have Locale value of {0}{reason}, but found {1} instead",
                    expectation.Locale, subject.Locale);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.Namespace)))
        {
            assertionChain
                .ForCondition(subject.Namespace == expectation.Namespace)
                .FailWith("Expected {context:DataTable} to have Namespace value of {0}{reason}, but found {1} instead",
                    expectation.Namespace, subject.Namespace);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.Prefix)))
        {
            assertionChain
                .ForCondition(subject.Prefix == expectation.Prefix)
                .FailWith("Expected {context:DataTable} to have Prefix value of {0}{reason}, but found {1} instead",
                    expectation.Prefix, subject.Prefix);
        }

        if (selectedMembers.ContainsKey(nameof(expectation.RemotingFormat)))
        {
            assertionChain
                .ForCondition(subject.RemotingFormat == expectation.RemotingFormat)
                .FailWith("Expected {context:DataTable} to have RemotingFormat value of {0}{reason}, but found {1} instead",
                    expectation.RemotingFormat, subject.RemotingFormat);
        }
    }

    private static void CompareCollections(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency parent, IEquivalencyOptions config, Dictionary<string, IMember> selectedMembers,
        AssertionChain assertionChain)
    {
        // Note: The collections here are listed in the XML documentation for the DataTable.BeEquivalentTo extension
        // method in DataTableAssertions.cs. If this ever needs to change, keep them in sync.
        var collectionNames = new[]
        {
            nameof(DataTable.ChildRelations),
            nameof(DataTable.Columns),
            nameof(DataTable.Constraints),
            nameof(DataTable.ExtendedProperties),
            nameof(DataTable.ParentRelations),
            nameof(DataTable.PrimaryKey),
            nameof(DataTable.Rows),
        };

        foreach (var collectionName in collectionNames)
        {
            if (selectedMembers.TryGetValue(collectionName, out IMember expectationMember))
            {
                IMember matchingMember = FindMatchFor(expectationMember, comparands.Subject, context.CurrentNode, config,
                    assertionChain);

                if (matchingMember is not null)
                {
                    IEquivalencyValidationContext nestedContext = context.AsNestedMember(expectationMember);

                    var nestedComparands = new Comparands
                    {
                        Subject = matchingMember.GetValue(comparands.Subject),
                        Expectation = expectationMember.GetValue(comparands.Expectation),
                        CompileTimeType = expectationMember.Type
                    };

                    parent.AssertEquivalencyOf(nestedComparands, nestedContext);
                }
            }
        }
    }

    private static IMember FindMatchFor(IMember selectedMemberInfo, object subject, INode currentNode,
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

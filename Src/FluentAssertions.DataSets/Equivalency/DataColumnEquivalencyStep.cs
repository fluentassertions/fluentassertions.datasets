using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class DataColumnEquivalencyStep : EquivalencyStep<DataColumn>
{
    [SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "The code is easier to read without it.")]
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);
        var subject = comparands.Subject as DataColumn;

        if (comparands.Expectation is not DataColumn expectation)
        {
            if (subject is not null)
            {
                assertionChain.FailWith("Expected {context:DataColumn} value to be null, but found {0}", subject);
            }
        }
        else if (subject is null)
        {
            if (comparands.Subject is null)
            {
                assertionChain.FailWith("Expected {context:DataColumn} to be non-null, but found null");
            }
            else
            {
                assertionChain.FailWith("Expected {context:DataColumn} to be of type {0}, but found {1} instead",
                    expectation.GetType(), comparands.Subject.GetType());
            }
        }
        else
        {
            CompareSubjectAndExpectationOfTypeDataColumn(comparands, context, nestedValidator, subject, assertionChain);
        }

        return EquivalencyResult.EquivalencyProven;
    }

    private static void CompareSubjectAndExpectationOfTypeDataColumn(Comparands comparands,
        IEquivalencyValidationContext context, IValidateChildNodeEquivalency parent, DataColumn subject, AssertionChain assertionChain)
    {
        bool compareColumn = true;

        var dataSetConfig = context.Options as DataEquivalencyAssertionOptions<DataSet>;
        var dataTableConfig = context.Options as DataEquivalencyAssertionOptions<DataTable>;
        var dataColumnConfig = context.Options as DataEquivalencyAssertionOptions<DataColumn>;

        if (dataSetConfig?.ShouldExcludeColumn(subject) == true
            || dataTableConfig?.ShouldExcludeColumn(subject) == true
            || dataColumnConfig?.ShouldExcludeColumn(subject) == true)
        {
            compareColumn = false;
        }

        if (compareColumn)
        {
            foreach (IMember expectationMember in GetMembersFromExpectation(context.CurrentNode, comparands, context.Options))
            {
                if (expectationMember.Name != nameof(subject.Table))
                {
                    CompareMember(expectationMember, comparands, parent, context, assertionChain);
                }
            }
        }
    }

    private static void CompareMember(IMember expectationMember, Comparands comparands, IValidateChildNodeEquivalency parent,
        IEquivalencyValidationContext context, AssertionChain assertionChain)
    {
        IMember matchingMember = FindMatchFor(expectationMember, comparands.Subject, context, assertionChain);

        if (matchingMember is not null)
        {
            var nestedComparands = new Comparands
            {
                Subject = matchingMember.GetValue(comparands.Subject),
                Expectation = expectationMember.GetValue(comparands.Expectation),
                CompileTimeType = expectationMember.Type
            };

            if (context.AsNestedMember(expectationMember) is not null)
            {
                parent.AssertEquivalencyOf(nestedComparands, context.AsNestedMember(expectationMember));
            }
        }
    }

    private static IMember FindMatchFor(IMember selectedMemberInfo, object subject, IEquivalencyValidationContext context,
        AssertionChain assertionChain)
    {
        IEnumerable<IMember> query =
            from rule in context.Options.MatchingRules
            let match = rule.Match(selectedMemberInfo, subject, context.CurrentNode, context.Options, assertionChain)
            where match is not null
            select match;

        return query.FirstOrDefault();
    }

    // NOTE: This list of candidate members is duplicated in the XML documentation for the
    // DataColumn.BeEquivalentTo extension method in DataColumnAssertions.cs. If this ever
    // needs to change, keep them in sync.
    private static readonly HashSet<string> CandidateMembers = new()
    {
        nameof(DataColumn.AllowDBNull),
        nameof(DataColumn.AutoIncrement),
        nameof(DataColumn.AutoIncrementSeed),
        nameof(DataColumn.AutoIncrementStep),
        nameof(DataColumn.Caption),
        nameof(DataColumn.ColumnName),
        nameof(DataColumn.DataType),
        nameof(DataColumn.DateTimeMode),
        nameof(DataColumn.DefaultValue),
        nameof(DataColumn.Expression),
        nameof(DataColumn.ExtendedProperties),
        nameof(DataColumn.MaxLength),
        nameof(DataColumn.Namespace),
        nameof(DataColumn.Prefix),
        nameof(DataColumn.ReadOnly),
        nameof(DataColumn.Unique),
    };

    private static IEnumerable<IMember> GetMembersFromExpectation(INode currentNode, Comparands comparands,
        IEquivalencyOptions config)
    {
        IEnumerable<IMember> members = Enumerable.Empty<IMember>();

        foreach (IMemberSelectionRule rule in config.SelectionRules)
        {
            members = rule.SelectMembers(currentNode, members,
                new MemberSelectionContext(comparands.CompileTimeType, comparands.RuntimeType, config));
        }

        return members.Where(member => CandidateMembers.Contains(member.Name));
    }
}

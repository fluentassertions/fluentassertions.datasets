﻿using System;
using System.Data;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class DataRowCollectionEquivalencyStep : EquivalencyStep<DataRowCollection>
{
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);

        if (comparands.Subject is not DataRowCollection)
        {
            assertionChain
                .FailWith("Expected {context:value} to be of type DataRowCollection, but found {0}",
                    comparands.Subject.GetType());
        }
        else
        {
            RowMatchMode rowMatchMode = context.Options switch
            {
                DataEquivalencyAssertionOptions<DataSet> dataSetConfig => dataSetConfig.RowMatchMode,
                DataEquivalencyAssertionOptions<DataTable> dataTableConfig => dataTableConfig.RowMatchMode,
                _ => RowMatchMode.Index
            };

            var subject = (DataRowCollection)comparands.Subject;
            var expectation = (DataRowCollection)comparands.Expectation;

            assertionChain
                .ForCondition(subject.Count == expectation.Count)
                .FailWith("Expected {context:DataRowCollection} to contain {0} row(s){reason}, but found {1}",
                    expectation.Count, subject.Count);

            if (assertionChain.Succeeded)
            {
                switch (rowMatchMode)
                {
                    case RowMatchMode.Index:
                        MatchRowsByIndexAndCompare(context, nestedValidator, subject, expectation);
                        break;

                    case RowMatchMode.PrimaryKey:
                        MatchRowsByPrimaryKeyAndCompare(nestedValidator, context, subject, expectation, assertionChain);
                        break;

                    default:
                        assertionChain.FailWith(
                            "Unknown RowMatchMode {0} when trying to compare {context:DataRowCollection}", rowMatchMode);

                        break;
                }
            }
        }

        return EquivalencyResult.EquivalencyProven;
    }

    private static void MatchRowsByIndexAndCompare(IEquivalencyValidationContext context, IValidateChildNodeEquivalency parent,
        DataRowCollection subject, DataRowCollection expectation)
    {
        for (int index = 0; index < expectation.Count; index++)
        {
            IEquivalencyValidationContext nestedContext = context.AsCollectionItem<DataRow>(index);
            parent.AssertEquivalencyOf(new Comparands(subject[index], expectation[index], typeof(DataRow)), nestedContext);
        }
    }

    private static void MatchRowsByPrimaryKeyAndCompare(IValidateChildNodeEquivalency parent,
        IEquivalencyValidationContext context,
        DataRowCollection subject, DataRowCollection expectation, AssertionChain assertionChain)
    {
        Type[] subjectPrimaryKeyTypes = null;
        Type[] expectationPrimaryKeyTypes = null;

        if (subject.Count > 0)
        {
            subjectPrimaryKeyTypes = GatherPrimaryKeyColumnTypes(subject[0].Table, "subject", assertionChain);
        }

        if (expectation.Count > 0)
        {
            expectationPrimaryKeyTypes = GatherPrimaryKeyColumnTypes(expectation[0].Table, "expectation", assertionChain);
        }

        bool matchingTypes = ComparePrimaryKeyTypes(subjectPrimaryKeyTypes, expectationPrimaryKeyTypes, assertionChain);

        if (matchingTypes)
        {
            GatherRowsByPrimaryKeyAndCompareData(parent, context, subject, expectation, assertionChain);
        }
    }

    private static Type[] GatherPrimaryKeyColumnTypes(DataTable table, string comparisonTerm, AssertionChain assertionChain)
    {
        Type[] primaryKeyTypes = null;

        if (table.PrimaryKey is null or { Length: 0 })
        {
            assertionChain
                .FailWith(
                    "Table {0} containing {1} {context:DataRowCollection} does not have a primary key. RowMatchMode.PrimaryKey cannot be applied.",
                    table.TableName, comparisonTerm);
        }
        else
        {
            primaryKeyTypes = new Type[table.PrimaryKey.Length];

            for (int i = 0; i < table.PrimaryKey.Length; i++)
            {
                primaryKeyTypes[i] = table.PrimaryKey[i].DataType;
            }
        }

        return primaryKeyTypes;
    }

    private static bool ComparePrimaryKeyTypes(Type[] subjectPrimaryKeyTypes, Type[] expectationPrimaryKeyTypes, AssertionChain assertionChain)
    {
        bool matchingTypes = false;

        if (subjectPrimaryKeyTypes is not null && expectationPrimaryKeyTypes is not null)
        {
            matchingTypes = subjectPrimaryKeyTypes.Length == expectationPrimaryKeyTypes.Length;

            for (int i = 0; matchingTypes && i < subjectPrimaryKeyTypes.Length; i++)
            {
                if (subjectPrimaryKeyTypes[i] != expectationPrimaryKeyTypes[i])
                {
                    matchingTypes = false;
                }
            }

            if (!matchingTypes)
            {
                assertionChain
                    .FailWith(
                        "Subject and expectation primary keys of table containing {context:DataRowCollection} do not have the same schema and cannot be compared. RowMatchMode.PrimaryKey cannot be applied.");
            }
        }

        return matchingTypes;
    }

    private static void GatherRowsByPrimaryKeyAndCompareData(IValidateChildNodeEquivalency parent, IEquivalencyValidationContext context,
        DataRowCollection subject, DataRowCollection expectation, AssertionChain assertionChain)
    {
        var expectationRowByKey = expectation.Cast<DataRow>()
            .ToDictionary(row => ExtractPrimaryKey(row));

        foreach (DataRow subjectRow in subject.Cast<DataRow>())
        {
            CompoundKey key = ExtractPrimaryKey(subjectRow);

            if (!expectationRowByKey.TryGetValue(key, out DataRow expectationRow))
            {
                assertionChain
                    .FailWith("Found unexpected row in {context:DataRowCollection} with key {0}", key);
            }
            else
            {
                expectationRowByKey.Remove(key);

                IEquivalencyValidationContext nestedContext = context.AsCollectionItem<DataRow>(key.ToString());
                parent.AssertEquivalencyOf(new Comparands(subjectRow, expectationRow, typeof(DataRow)), nestedContext);
            }
        }

        if (expectationRowByKey.Count > 0)
        {
            if (expectationRowByKey.Count > 1)
            {
                assertionChain
                    .FailWith("{0} rows were expected in {context:DataRowCollection} and not found", expectationRowByKey.Count);
            }
            else
            {
                assertionChain
                    .FailWith(
                        "Expected to find a row with key {0} in {context:DataRowCollection}{reason}, but no such row was found",
                        expectationRowByKey.Keys.Single());
            }
        }
    }

    private sealed class CompoundKey : IEquatable<CompoundKey>
    {
        private readonly object[] values;

        public CompoundKey(params object[] values)
        {
            this.values = values;
        }

        public bool Equals(CompoundKey other)
        {
            if (other is null)
            {
                return false;
            }

            return values.Length == other.values.Length && values.SequenceEqual(other.values);
        }

        public override bool Equals(object obj) => Equals(obj as CompoundKey);

        public override int GetHashCode()
        {
            int hash = 0;

            foreach (var value in values)
            {
                hash = hash * 389 ^ value.GetHashCode();
            }

            return hash;
        }

        public override string ToString()
        {
            return "{ " + string.Join(", ", values) + " }";
        }
    }

    private static CompoundKey ExtractPrimaryKey(DataRow row)
    {
        DataColumn[] primaryKey = row.Table.PrimaryKey;

        var values = new object[primaryKey.Length];

        for (int i = 0; i < values.Length; i++)
        {
            values[i] = row[primaryKey[i]];
        }

        return new CompoundKey(values);
    }
}

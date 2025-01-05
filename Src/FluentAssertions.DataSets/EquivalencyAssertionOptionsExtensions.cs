using System.Data;
using System.Linq;
using FluentAssertions.DataSets.Equivalency;
using FluentAssertions.Equivalency;
using FluentAssertions.Equivalency.Steps;

namespace FluentAssertions.DataSets;

public static class EquivalencyAssertionOptionsExtensions
{
    /// <summary>
    /// Enables support for comparing object graphs containing <see cref="DataSet"/> and its related types.
    /// </summary>
    public static void AddDataSetSupport(this EquivalencyPlan plan)
    {
        if (!plan.Any(x => x is DataSetEquivalencyStep))
        {
            plan.AddAfter<GenericDictionaryEquivalencyStep, DataSetEquivalencyStep>();
            plan.AddAfter<DataSetEquivalencyStep, DataTableEquivalencyStep>();
            plan.AddAfter<DataTableEquivalencyStep, DataColumnEquivalencyStep>();
            plan.AddAfter<DataColumnEquivalencyStep, DataRelationEquivalencyStep>();
            plan.AddAfter<DataRelationEquivalencyStep, DataRowCollectionEquivalencyStep>();
            plan.AddAfter<DataRowCollectionEquivalencyStep, DataRowEquivalencyStep>();
            plan.AddAfter<XAttributeEquivalencyStep, ConstraintCollectionEquivalencyStep>();
            plan.AddAfter<ConstraintCollectionEquivalencyStep, ConstraintEquivalencyStep>();
        }
    }
}

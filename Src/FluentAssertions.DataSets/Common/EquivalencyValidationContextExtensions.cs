using System.Globalization;
using FluentAssertions.Equivalency;

namespace FluentAssertions.DataSets.Common;

internal static class EquivalencyValidationContextExtensions
{
    public static IEquivalencyValidationContext AsCollectionItem<TItem>(this IEquivalencyValidationContext context, int index) =>
        context.AsCollectionItem<TItem>(index.ToString(CultureInfo.InvariantCulture));
}

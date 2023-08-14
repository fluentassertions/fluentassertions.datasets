using FluentAssertions;
using FluentAssertions.DataSets;
using FluentAssertions.Extensibility;

[assembly: CustomAssertionsAssembly]

[assembly: AssertionEngineInitializer(
    typeof(InitializeDataSetSupport),
    nameof(InitializeDataSetSupport.Initialize))]

namespace FluentAssertions.DataSets;

public static class InitializeDataSetSupport
{
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
    public static void Initialize()
#pragma warning restore CA1822
    {
        AssertionOptions.EquivalencyPlan.AddDataSetSupport();
    }
}

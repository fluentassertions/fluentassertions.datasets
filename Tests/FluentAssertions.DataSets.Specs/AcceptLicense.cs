using FluentAssertions.DataSets.Specs;
using FluentAssertions.Extensibility;

[assembly: AssertionEngineInitializer(
    typeof(AcceptLicense),
    nameof(AcceptLicense.Initialize))]

namespace FluentAssertions.DataSets.Specs;

public static class AcceptLicense
{
    // ReSharper disable once UnusedMember.Global
#pragma warning disable CA1822
    public static void Initialize()
#pragma warning restore CA1822
    {
        License.Accepted = true;
    }
}

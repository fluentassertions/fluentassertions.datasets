using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentAssertions.DataSets.Common;
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;

namespace FluentAssertions.DataSets.Equivalency;

public class ConstraintCollectionEquivalencyStep : EquivalencyStep<ConstraintCollection>
{
    protected override EquivalencyResult OnHandle(Comparands comparands, IEquivalencyValidationContext context,
        IValidateChildNodeEquivalency nestedValidator)
    {
        var assertionChain = AssertionChain.GetOrCreate().For(context);

        if (comparands.Subject is not ConstraintCollection)
        {
            assertionChain
                .FailWith("Expected a value of type ConstraintCollection at {context:Constraints}, but found {0}",
                    comparands.Subject.GetType());
        }
        else
        {
            var subject = (ConstraintCollection)comparands.Subject;
            var expectation = (ConstraintCollection)comparands.Expectation;

            var subjectConstraints = subject.Cast<Constraint>().ToDictionary(constraint => constraint.ConstraintName);
            var expectationConstraints = expectation.Cast<Constraint>().ToDictionary(constraint => constraint.ConstraintName);

            IEnumerable<string> constraintNames = subjectConstraints.Keys.Union(expectationConstraints.Keys);

            foreach (var constraintName in constraintNames)
            {
                assertionChain
                    .ForCondition(subjectConstraints.TryGetValue(constraintName, out Constraint subjectConstraint))
                    .FailWith("Expected constraint named {0} in {context:Constraints collection}{reason}, but did not find one",
                        constraintName);

                assertionChain
                    .ForCondition(expectationConstraints.TryGetValue(constraintName, out Constraint expectationConstraint))
                    .FailWith("Found unexpected constraint named {0} in {context:Constraints collection}", constraintName);

                if (subjectConstraint is not null && expectationConstraint is not null)
                {
                    Comparands newComparands = new()
                    {
                        Subject = subjectConstraint,
                        Expectation = expectationConstraint,
                        CompileTimeType = typeof(Constraint)
                    };

                    IEquivalencyValidationContext nestedContext = context.AsCollectionItem<Constraint>(constraintName);
                    nestedValidator.AssertEquivalencyOf(newComparands, nestedContext);
                }
            }
        }

        return EquivalencyResult.EquivalencyProven;
    }
}

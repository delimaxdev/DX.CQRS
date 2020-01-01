using FluentAssertions;

namespace DX.Testing
{
    public class Feature : FeatureBase {
        static Feature() {
            FilePath.SetSolutionPath(c => c.RelativeToBaseDirectory(@"..\..\..\..\"));
            AssertionOptions.EquivalencySteps.Insert<BsonDocumentEquivalencyStep>();
        }
    }
}
using FluentAssertions.Equivalency;
using FluentAssertions.Execution;
using FluentAssertions.Primitives;
using MongoDB.Bson;
using System.Linq;

namespace FluentAssertions {
    public static class BsonTestExtensions {
        public static BsonDcoumentAssertions Should(this BsonDocument doc) {
            return new BsonDcoumentAssertions(doc);
        }
    }

    internal class BsonDocumentEquivalencyStep : IEquivalencyStep {
        public bool CanHandle(IEquivalencyValidationContext context, IEquivalencyAssertionOptions config) {
            return context.Subject is BsonDocument && context.Expectation is BsonDocument;
        }

        public bool Handle(IEquivalencyValidationContext context, IEquivalencyValidator parent, IEquivalencyAssertionOptions config) {
            BsonDocument subject = (BsonDocument)context.Subject;
            BsonDocument expected = (BsonDocument)context.Expectation;

            IsEquivalent(subject, expected).Should().BeTrue("Expected document to be\n{0}\nbut found\n{1}.", expected, subject);

            return true;
        }

        internal static bool IsEquivalent(BsonDocument first, BsonDocument second) {
            bool hasSameElements = first
                .Select(x => x.Name)
                .ToHashSet()
                .SetEquals(second.Select(x => x.Name));

            if (!hasSameElements)
                return false;

            return first.Elements
                .Join(second.Elements, x => x.Name, x => x.Name, (f, s) => IsEquivalent(f.Value, s.Value))
                .All(x => x);
        }

        internal static bool IsEquivalent(BsonValue first, BsonValue second) {
            if (first.BsonType != second.BsonType)
                return false;

            if (first.IsBsonDocument)
                return IsEquivalent(first.AsBsonDocument, second.AsBsonDocument);

            if (first.IsBsonArray) {
                BsonValue[] firstValues = first.AsBsonArray.Values.ToArray();
                BsonValue[] secondValues = second.AsBsonArray.Values.ToArray();

                if (firstValues.Length != secondValues.Length)
                    return false;

                for (int i = 0; i < firstValues.Length; i++) {
                    if (!IsEquivalent(firstValues[i], secondValues[i]))
                        return false;
                }

                return true;
            }

            return first.Equals(second);
        }
    }

    // TODO: Test this class!!
    public class BsonDcoumentAssertions :
        ReferenceTypeAssertions<BsonDocument, BsonDcoumentAssertions> {

        public BsonDcoumentAssertions(BsonDocument instance) {
            Subject = instance;
        }

        protected override string Identifier => "document";

        public AndConstraint<BsonDcoumentAssertions> HaveElement(string name) {
            Execute.Assertion
                .ForCondition(Subject.IndexOfName(name) >= 0)
                .FailWith("Expected document to contain element {0}. Document:\n{1}.", name, Subject);

            return new AndConstraint<BsonDcoumentAssertions>(this);
        }

        public AndConstraint<BsonDcoumentAssertions> HaveElement(string name, BsonValue value) {
            HaveElement(name);

            Execute.Assertion
                .ForCondition(BsonDocumentEquivalencyStep.IsEquivalent(value, Subject.GetValue(name)))
                .FailWith("Expeced element {0} to have value {1} but found {2}.", name, value, Subject.GetValue(name));

            return new AndConstraint<BsonDcoumentAssertions>(this);
        }

        public void BeEqivalentTo(BsonDocument expected) {
            new ObjectAssertions(Subject).BeEquivalentTo(expected);
        }
    }
}
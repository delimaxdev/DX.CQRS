using DX.Contracts;
using DX.Testing;
using FluentAssertions;
using Xbehave;

namespace Domain {
    public class RefFeature : Feature {

        [Scenario]
        internal void Tests(
            SampleObject instance,
            Ref<SampleObject> r,
            Ref<SampleObject> second,
            Ref<ISampleObject> ir,
            Ref<UnrelatedObject> unrelated,
            ID id
        ) {
            GIVEN["an object instance"] = () => instance = new SampleObject { ID = id = ID.NewID() };
            THEN["it can be assigned to a base ref"] = () => ir = instance;
            WHEN["assigning it to a Ref"] = () => r = instance;
            THEN["the Ref has its ID"] = () => ID.FromRef(r).Should().Be(instance.ID);

            WHEN["creating a Ref from an ID"] = () => second = id.ToRef<SampleObject>();
            THEN["the Ref is equal to the original"] = () => second.Should().Be(r);
            THEN["the Ref as the specified ID"] = () => ID.FromRef(second).Should().Be(id);

            WHEN["downcasting a Ref"] = () => ir = r.Cast<ISampleObject>();
            THEN["it is equal to original"] = () => ir.Should().Be(r);

            WHEN["upcasting the Ref"] = () => r = ir.Cast<SampleObject>();
            THEN["it is equal to the orignal"] = () => r.Should().Be(ir);
            AND["its still has the same ID"] = () => ID.FromRef(r).Should().Be(id);

            GIVEN["a unrelated Ref with the same ID"] = () => unrelated = id.ToRef<UnrelatedObject>();
            THEN["it is not equal to the orignal"] = () => unrelated.Should().NotBe(r);
            AND["the original is not equal to the unrelated Ref"] = () => r.Should().NotBe(unrelated);
        }

        internal class SampleObject : ISampleObject {
            public ID ID { get; set; }
        }

        internal class UnrelatedObject : IHasID<ID> {
            public ID ID { get; set; }
        }

        internal interface ISampleObject : IHasID<ID> { }
    }
}
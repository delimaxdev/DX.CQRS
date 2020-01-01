using DX.Contracts;
using DX.Cqrs.Domain;
using DX.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xbehave;

namespace Domain {
    [ContractContainer]
    public class SerializationFeature : Feature {
        #region StreamNames

        [Scenario(Skip = "Migrate to new impl")]
        internal void StreamNames(ConfiguratorFake c) {
            WHEN["scanning a class with StreamAttribute and Name"] = () => c = Scan(typeof(SampleStreamWithName)); ;
            THEN["its configured name is used"] = () => c.StreamName.Should().Be("TestName");

            WHEN["scanning a class with StreamAttribute without Name"] = () => c = Scan(typeof(SampleStream)); ;
            THEN["its class name is used"] = () => c.StreamName.Should().Be("SampleStream");
        }

        [Stream("TestName")]
        class SampleStreamWithName { }

        [Stream]
        class SampleStream { }

        #endregion

        #region EventNames

        [Scenario(Skip = "Migrate to new impl")]
        [Example(typeof(ContainerClass.EventWithoutName), "ContainerClassEventWithoutName")]
        [Example(typeof(ContainerClass.EventWithPostfix), "ContainerClassTestPostfix")]
        [Example(typeof(ContainerClass.EventWithName), "TestName")]
        [Example(typeof(ContainerClassWithPrefix.EventWithoutName), "TestPrefixEventWithoutName")]
        [Example(typeof(ContainerClassWithPrefix.EventWithPostfix), "TestPrefixTestPostfix")]
        [Example(typeof(ContainerClassWithPrefix.EventWithName), "TestName")]
        internal void EventNames(Type eventType, string expectedName, ConfiguratorFake c) {
            WHEN["scaning a certain event"] = () => c = Scan(eventType);
            THEN["RegisterEventClass is called with the expected name"] = () => c.ClassName.Should().Be(expectedName);
        }

        [Scenario(Skip = "Migrate to new impl")]
        internal void ScanningForNestedClasses(ConfiguratorFake c) {
            WHEN["scanning a type that contains nested event classes"] = () => c = Scan(typeof(ContainerClass));
            THEN["the nested classes are also found"] = () => c.ClassNames.Should().Contain("TestName");
        }

        [ContractContainer]
        class ContainerClass {
            [Contract("TestName")]
            public class EventWithName { }

            [Contract(PartialName = "TestPostfix")]
            public class EventWithPostfix { }

            [Contract]
            public class EventWithoutName { }
        }

        [ContractContainer(Name = "TestPrefix")]
        class ContainerClassWithPrefix {
            [Contract("TestName")]
            public class EventWithName { }

            [Contract(PartialName = "TestPostfix")]
            public class EventWithPostfix { }

            [Contract]
            public class EventWithoutName { }
        }

        #endregion

        #region PropertyMappings

        [Scenario(Skip = "Migrate to new impl")]
        internal void PropertyMappings(ConfiguratorFake c, ClassConfiguratorFake cc) {
            WHEN["scanning a class"] = () => cc = (c = Scan(typeof(SimpleEvent))).Class<SimpleEvent>();

            THEN["the Name attribute is considered"] = () =>
                cc.PropertyNames.Should().Contain("TestName");

            WHEN["scanning a class with well known property types"] = () => cc = Scan(typeof(EventWithProperties)).Class<EventWithProperties>();
            THEN["all public properties are mapped"] = () =>
                cc.PropertyNames.Should().BeEquivalentTo("StringProperty", "UriProperty", "ListProperty", "StructProperty", "ObjectProperty");

            WHEN["scanning a derived class"] = () => c = Scan(typeof(DerivedEvent), typeof(BaseEvent), typeof(TestEvent));
            THEN["only the declared properties are mapped"] = () => {
                c.Class<BaseEvent>().PropertyNames.Should().Contain(nameof(BaseEvent.BaseProperty));
                c.Class<DerivedEvent>().PropertyNames.Should().Contain(nameof(DerivedEvent.DerivedProperty));
            };

            WHEN["scanning an event with a data class property"] = () => cc = Scan(typeof(EventWithDataClass), typeof(DataClass1)).Class<EventWithDataClass>();
            THEN["it is correctly mapped"] = () => cc.PropertyNames.Should().BeEquivalentTo(nameof(EventWithDataClass.DataClassProperty));

            WHEN["a data class references a type that is not marked as data class"] = null;
            THEN["Scan", ThrowsA<ConfigurationException>()] = () =>
                c = Scan(typeof(ClassWithInvalidProperty), typeof(ClassWithoutContractAttribute));
        }

        [Contract]
        class SimpleEvent : IEvent {
            [ContractMember(Name = "TestName")]
            public string PropertyWithNameAttribute { get; set; }
        }

        [Contract]
        class EventWithProperties : IEvent {
            public string StringProperty { get; set; }

            public Uri UriProperty { get; set; }

            public List<string> ListProperty { get; set; }

            public TestStruct StructProperty { get; set; }

            public object ObjectProperty { get; set; }

            private int PrivateProperty { get; set; }
        }

        [Contract]
        class EventWithDataClass : IEvent {
            public DataClass1 DataClassProperty { get; set; }
        }

        struct TestStruct { }

        [Contract]
        class BaseEvent {
            public string BaseProperty { get; set; }
        }

        [Contract]
        class DerivedEvent : BaseEvent {
            public string DerivedProperty { get; set; }
        }

        [Contract]
        class ClassWithInvalidProperty {
            public ClassWithoutContractAttribute Nested { get; set; }
        }

        class ClassWithoutContractAttribute {

        }

        #endregion

        #region GenericEvents

        [Scenario(Skip = "Migrate to new impl")]
        internal void GenericEvents(ConfiguratorFake c) {
            WHEN["scanning a generic event"] = () => c = Scan(typeof(GenericEvent<>), typeof(DataClass1), typeof(DataClass2));
            THEN["RegisterGenericArgument is called"] = () =>
                c.Class(typeof(GenericEvent<>)).GenericArguments.Should().BeEquivalentTo(typeof(DataClass1), typeof(DataClass2));

            WHEN["scanning a generic event without GenericArguments", ThenIsThrown<ConfigurationException>()] = () =>
                Scan(typeof(GenericClassWithoutArgs<string>));

            WHEN["scanning a generic class without a Name"] = () => c = Scan(typeof(GenericEvent<>), typeof(DataClass1), typeof(DataClass2));
            THEN["it is registered with its simple class name"] = () =>
                c.ClassNames.Should().Contain("SerializationFeatureGenericEvent");
        }

        [Contract]
        class GenericClassWithoutArgs<T> : TestEvent {
        }

        [Contract(GenericArguments = new[] { typeof(DataClass1), typeof(DataClass2) })]
        class GenericEvent<T> : TestEvent {

        }

        [Contract]
        class DataClass1 {
            [ContractMember("TestName")]
            public string PropertyWithNameAttribute { get; set; }
        }

        [Contract]
        class DataClass2 {

        }

        #endregion


        #region Test infrastructure

        private ConfiguratorFake Scan(params Type[] types) {
            var c = new ConfiguratorFake();
            return c;
        }


        internal class ConfiguratorFake {
            private readonly Dictionary<Type, ClassConfiguratorFake> _classes = new Dictionary<Type, ClassConfiguratorFake>();

            public string StreamName { get; private set; }

            public List<string> ClassNames { get; } = new List<string>();

            public string ClassName => ClassNames.Last();

            public ClassConfiguratorFake Class<T>() {
                return _classes[typeof(T)];
            }

            public ClassConfiguratorFake Class(Type t)
                => _classes[t];
        }

        internal class ClassConfiguratorFake {
            public List<string> PropertyNames { get; } = new List<string>();

            public List<Type> GenericArguments { get; } = new List<Type>();
        }

        #endregion
    }
}

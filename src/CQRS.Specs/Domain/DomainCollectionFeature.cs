using DX.Collections;
using DX.Contracts;
using DX.Cqrs.Domain;
using DX.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Xbehave;

namespace Domain {
    public class DomainCollectionFeature : Feature {
        [Scenario]
        internal void DomainCollectionTests(Messenger m, IKeyed<int, PersonDO> collection) {
            GIVEN["a collection and a Messenger"] = () =>
                collection = collection.Create(m = CreateMesseneger(), x => x.ID, x => {
                    x.Apply<PersonAdded>((e, c) => c.Add(e));
                    x.Apply<PersonChanged>((e, c) => c.Set(e.ID, e));
                    x.Apply<PersonRemoved>((e, c) => c.Remove(e.ID));
                });

            WHEN["sending an add event"] = () => m.ApplyChange(new PersonAdded { ID = 1, Name = "N1" });
            THEN["the item is added"] = () => collection.Should().BeEquivalentTo(new PersonDO { ID = 1, Name = "N1" });

            WHEN["sending a set event"] = () => m.ApplyChange(new PersonChanged { ID = 1, Name = "N2" });
            THEN["the item is replaced"] = () => collection.Should().BeEquivalentTo(new PersonDO { ID = 1, Name = "N2" });

            WHEN["sending a remove event"] = () => m.ApplyChange(new PersonRemoved { ID = 1});
            THEN["the item is added"] = () => collection.Should().BeEquivalentTo();
        }

        private Messenger CreateMesseneger()
            => new TestAggregate().Messenger;

        internal class TestAggregate : AggregateRoot {
            public Messenger Messenger => M;
        }

        internal class PersonDO {
            public int ID { get; set; }
            public string Name { get; set; }
        }

        internal class PersonAdded : PersonDO, IEvent { }

        internal class PersonChanged : PersonDO, IEvent { }

        internal class PersonRemoved : IEvent {
            public int ID { get; set; }
        }
    }
}
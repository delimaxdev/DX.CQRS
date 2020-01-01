using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using DX.Cqrs.EventStore;
using DX.Testing;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xbehave;

namespace Domain {
    public class RepositoryFeature : Feature {

        [Scenario(DisplayName = "Save and Get")]
        public void SaveAndGet(
            TestEventStore store,
            Repository<Customer> repo,
            Customer c,
            Customer duplicate,
            Customer invalid,
            List<IEvent> allChanges
        ) {
            GIVEN["a TestEventStore"] = () => store = new TestEventStore();
            GIVEN["a repository"] = () => {
                IEventMetadataProvider provider = Substitute.For<IEventMetadataProvider>();
                provider.Provide().Returns(new DefaultEventMetadata(DateTime.Now));
                repo = new Repository<Customer>(store.UseTransaction(null), provider);
            };

            AND["a new customer with pending changes"] = () => {
                IEvent[] changes = { new Customer.Created(), new Customer.Promoted() };
                allChanges = new List<IEvent>(changes);

                c = Substitute.For<Customer>(ID.NewID());
                c.SetupGetChanges(isNew: true, changes);
            };

            When["calling Save"] = () => Save(repo, c);

            THEN["the changes are stored in the event store"] = () =>
                store.All.Single().Events.Should().BeEquivalentTo(c.RecordedChanges);

            AND["ClearChanges was called"] = () => c.Received().ClearChanges();

            GIVEN["an existing customer with changes"] = () => {
                IEvent[] changes = { new Customer.AddressChanged() };
                allChanges.AddRange(changes);

                c.SetupGetChanges(isNew: false, changes);
            };

            When["calling Save"] = () => Save(repo, c);

            THEN["the changes are stored in the event store"] = () =>
                store.All.ElementAt(1).Events.Should().BeEquivalentTo(c.RecordedChanges);

            GIVEN["a new customer with an existing ID"] = () => {
                duplicate = new Customer(c.ID);
                duplicate.SetupGetChanges(isNew: true);
            };

            Then["calling Save", ThrowsA<InvalidOperationException>()] = () => Save(repo, duplicate);

            GIVEN["a new customer where Changeset.IsNew returns false"] = () => {
                invalid = new Customer(ID.NewID());
                invalid.SetupGetChanges(isNew: false);
            };

            Then["calling Save", ThrowsA<InvalidOperationException>()] = () => Save(repo, invalid);

            When["saving a second customr"] = () => {
                Customer second = new Customer(ID.NewID());
                second.SetupGetChanges(isNew: true, new Customer.Created());
                return Save(repo, second);
            };

            And["getting the first object"] = async () => c = await repo.Get(c.ID);

            THEN["Restore was called with the original events"] = () =>
                c.RestoreArgument.Should().BeEquivalentTo(allChanges, o => o.WithStrictOrdering());
        }

        private static Task Save<T>(Repository<T> repo, T obj) where T : class, IPersistable {
            return repo.Save(obj);
        }

        private static Changeset CreateChangeset(bool isNew, params IEvent[] events)
            => new Changeset(events, isNew);


        public class Customer : IPersistable {
            private Changeset _getChangesSetup;

            public IEvent[] RestoreArgument { get; set; }

            public RecordedEvent[] RecordedChanges => _getChangesSetup
                .Changes
                .Select(e => new RecordedEvent(ID, e, new object()))
                .ToArray();

            public ID ID { get; private set; }

            private Customer() { }

            public Customer(ID id) => ID = id;

            public virtual void ClearChanges() { }

            public Changeset GetChanges()
                => _getChangesSetup;

            public void Restore(ID id, IEnumerable<IEvent> events) {
                ID = id;
                RestoreArgument = events.ToArray();
            }

            public void SetupGetChanges(bool isNew, params IEvent[] changes) {
                _getChangesSetup = new Changeset(changes, isNew);
            }


            public class Created : IEvent { }

            public class Promoted : IEvent { }

            public class AddressChanged : IEvent { }
        }
    }
}

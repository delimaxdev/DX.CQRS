using DX.Contracts;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using DX.Testing;
using FluentAssertions;
using System.Collections.Generic;
using Xbehave;
using AggregateRoot = DX.Cqrs.Domain.AggregateRoot;

namespace Domain {
    public class AggregateRootFeature : Feature {
        [Scenario]
        public void SaveAndRestore(Client c, ID id, List<IEvent> all) {
            GIVEN["an aggregate root"] = () => c = new Client("CLIENT1", id = ID.NewID());

            WHEN["applying a change"] = () => c.CropCertifications.Add("CROP1");
            THEN["it is added to Changes and its StreamID is set"] = () => c.Changeset.Changes
                .Should().ContainEquivalentOf(new ObjectCertifications<Crop>.Added { Name = "CROP1" });
            AND["Changeset.IsNew is true"] = () => c.Changeset.IsNew.Should().BeTrue();

            WHEN["clearing all changes"] = () => {
                all = new List<IEvent>(c.Changeset.Changes);
                ((IPersistable)c).ClearChanges();
            };

            AND["applying another change"] = () => c.ProductCertifications.Add("PROD1");
            THEN["it is added to changes"] = () => {
                c.Changeset.Changes.Should().BeEquivalentTo(new[] { new ObjectCertifications<Product>.Added { Name = "PROD1" } });
                all.AddRange(c.Changeset.Changes);
            };
            AND["Changeset.IsNew returns false"] = () => c.Changeset.IsNew.Should().BeFalse();

            WHEN["restoring an aggregate root from all events"] = () => {
                c = new Client();
                ((IPersistable)c).Restore(id, all);
            };
            THEN["its ID is set"] = () => c.ID.Should().Be(id);
            THEN["all events are applied"] = () => {
                c.Name.Should().Be("CLIENT1");
                c.TotalCertifications.Should().Be(2);
                c.ProductCertifications.Objects.Should().HaveCount(1);
            };
            AND["Changes should be empty"] = () => c.Changeset.Changes.Should().BeEmpty();
            AND["Changeset.IsNew should be false"] = () => c.Changeset.IsNew.Should().BeFalse();

            WHEN["applying a change"] = () => c.CropCertifications.Add("CROP2");
            THEN["the change is added to the Changeset"] = () => c.Changeset.Changes
                .Should().BeEquivalentTo(new[] { new ObjectCertifications<Crop>.Added { Name = "CROP2" } });
            AND["Changeset.IsNew returns false"] = () => c.Changeset.IsNew.Should().BeFalse();
        }

        public class Client : AggregateRoot {
            public Client() {
                ProductCertifications = new ObjectCertifications<Product>(M);
                CropCertifications = new ObjectCertifications<Crop>(M);

                M.Apply<ObjectCertifications<Crop>.Added>(e => TotalCertifications += 1);
                M.Apply<ObjectCertifications<Product>.Added>(e => TotalCertifications += 1);
                M.Apply<Created>(e => Name = e.Name);
            }

            public Client(string name, ID id) : this() {
                ID = id;
                M.ApplyChange(new Created { Name = name });
            }

            public string Name { get; private set; }

            public ObjectCertifications<Product> ProductCertifications { get; }

            public ObjectCertifications<Crop> CropCertifications { get; }

            public int TotalCertifications { get; private set; }

            public Changeset Changeset => ((IPersistable)this).GetChanges();

            public new ID ID { get => base.ID; set => base.ID = value; }

            public class Created : TestEvent {
                public string Name { get; set; }
            }
        }

        public class ObjectCertifications<T> : DomainObject {
            public List<string> Objects { get; } = new List<string>();

            public ObjectCertifications(Messenger m) : base(m) {
                M.Apply<Added>(e => Objects.Add(e.Name));
            }

            public void Add(string name) {
                M.ApplyChange(new Added { Name = name });
            }

            public class Added : TestEvent {
                public string Name { get; set; }
            }
        }

        public class Product {

        }

        public class Crop {

        }
    }
}

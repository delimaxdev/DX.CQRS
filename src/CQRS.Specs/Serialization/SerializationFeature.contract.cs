using DX.Contracts;
using DX.Contracts.Domain;
using DX.Testing;
using MongoDB.Bson;
using System;
using System.Linq;

namespace Serialization {
    public partial class SerializationFeature {
        [Contract("UpdateObjects")]
        internal partial class UpdateCertificationObjects : ICommandMessage {
            [ContractMember("CertObjects")]
            public CertificationObjectDO[] Objects { get; }

            public ServiceCode? Service { get; }

            public BsonDocument GetExpectedBson() =>
                new BsonDocument {
                    { "_t", "TEST:Test.UpdateObjects" },
                    { "CertObjects", new BsonArray(Objects.Select(x => x.GetExpetedBson())) },
                    { "Service", Service != null ?
                            new BsonDocument {
                                { "_t", "TEST:Test.ServiceCode" },
                                { "Value", Service.Value} } :
                            null }
                };
        }

        [Contract]
        internal partial class CertificationObjectDO {
            public ID ID { get; }

            [ContractMember("Caption")]
            public string Name { get; }

            public BsonDocument GetExpetedBson() =>
                new BsonDocument {
                    { "ID", new BsonBinaryData(ID.ToGuid(ID), GuidRepresentation.Standard) },
                    { "Caption", Name }
                };
        }

        [Contract]
        internal class ServiceCode : IdentificationCode {
            public string Value { get; }

            public ServiceCode(string value) {
                Value = value;
            }

            protected override bool EqualsCore(IdentificationCode other)
                => other is ServiceCode c && c.Value == Value;

            protected override int GetHashCodeCore()
                => HashCode.Combine(Value);
        }
    }
}

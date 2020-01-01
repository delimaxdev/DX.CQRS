using DX.Collections;
using DX.Contracts;
using DX.Contracts.Domain;
using DX.Testing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

[assembly: ContractAssembly("TEST")]

namespace Serialization {
    public interface IObjectType : IHasID<ID> { }

    [ContractContainer("Test")]
    public partial class SerializationFeature {
        [Contract]
        public enum ObjectType {
            Crop,
            [EnumMember(Value = "Animals")] Livestock,
            Product
        }


        internal class NonContractClsas {
            public UpdateCertificationObjects ContractClassProperty { get; set; }

            [ContractMember("SHOULD_BE_IGNORED")]
            public string StringValue { get; set; }
        }

        [Contract]
        internal class InvalidContractClass {
            public NonContractClsas InvalidProperty { get; set; }
        }

        [Contract("UpdateObjects")]
        internal partial class UpdateCertificationObjects : ICommandMessage {
            [ContractMember("CertObjects")]
            public IKeyed<ID, CertificationObjectDO> Objects { get; }
            
            public ServiceCode? Service { get; }
            
            public JObject GetExpectedJson() =>
                new JObject {
                    { "_t", "TEST:Test.UpdateObjects" },
                    { "CertObjects", new JArray(Objects.Select(x => x.GetExpetedJson())) },
                    { "Service", Service != null ?
                        (JToken)new JObject {
                            { "_t", "TEST:Test.ServiceCode" },
                            { "Value", Service.Value} } :
                        null }
                };

            private void Initialize() {
                Builder.Set(Objects, new KeyedCollection<ID, CertificationObjectDO>(x => x.ID));
            }
        }

        [Contract]
        internal partial class CertificationObjectDO {
            public ID ID { get; }

            [ContractMember("Caption")]
            public string Name { get; }

            public ObjectType Type { get; }

            public Ref<IObjectType> TypeRef { get; }

            public JObject GetExpetedJson() =>
                new JObject {
                    { "ID", ID.ToGuid(ID) },
                    { "Caption", Name },
                    { "Type", Type == ObjectType.Livestock ?
                        "Animals" : Type.ToString()
                    },
                    // We automatically append ID at the end of Ref property names
                    { "TypeRefID", ID.ToGuid(ID.FromRef(TypeRef)) }
                };
        }

        [Contract]
        internal partial class StatusDO {
            public string Name { get; }

            public JObject GetExpectedJson() {
                return new JObject {
                    { "Name", Name }
                };
            }
        }

        [Contract]
        internal partial class UpdateServices : ICommandMessage {
            public IKeyedLookup<ID, StatusDO> Status { get; }

                        private UpdateServices() {
                Status = new KeyedLookup<ID, StatusDO>();
            }

            public JObject GetExpectedJson() {
                JObject statusJson = new JObject();
                foreach (KeyValuePair<ID, StatusDO> pair in Status.Pairs) {
                    statusJson.Add(pair.Key.ToString(), pair.Value.GetExpectedJson());
                }

                return new JObject {
                    { "_t", "TEST:Test.UpdateServices" },
                    { "Status", statusJson }
                };
            }

            private void Initialize() {
                Builder.Set(Status, new KeyedLookup<ID, StatusDO>());
            }
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

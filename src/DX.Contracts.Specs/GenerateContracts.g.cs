#nullable enable
namespace Serialization
{
    using System;
    using System.Collections.Generic;
    using DX;
    using DX.Contracts;
    using DX.Collections;
    using DX.Contracts;
    using DX.Contracts.Domain;
    using DX.Testing;
    using Newtonsoft.Json.Linq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    partial class SerializationFeature
    {
        [Builder(typeof(UpdateCertificationObjectsBuilder))]
        partial class UpdateCertificationObjects
        {
            public UpdateCertificationObjects(UpdateCertificationObjectsBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Objects = source.Objects.ToImmutable();
                Service = source.Service;
            }

            public UpdateCertificationObjects(UpdateCertificationObjects source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Objects = source.Objects;
                Service = source.Service;
            }
        }

        internal partial class UpdateCertificationObjectsBuilder : IBuilds<UpdateCertificationObjects>
        {
            [ContractMember("CertObjects")]
            public KeyedCollection<ID, CertificationObjectDO> Objects
            {
                get;
            }

            = new KeyedCollection<ID, CertificationObjectDO>(x => x.ID);
            public ServiceCode? Service
            {
                get;
                set;
            }

            public UpdateCertificationObjects Build()
            {
                return new UpdateCertificationObjects(this);
            }

            public static implicit operator UpdateCertificationObjects(UpdateCertificationObjectsBuilder builder)
            {
                return builder.Build();
            }

            public UpdateCertificationObjectsBuilder(UpdateCertificationObjects? source = null): base()
            {
                if (source != null)
                {
                    Objects.AddRange(source.Objects);
                    Service = source.Service;
                }
            }

            public UpdateCertificationObjectsBuilder(): base()
            {
            }
        }

        [Builder(typeof(CertificationObjectDOBuilder))]
        partial class CertificationObjectDO
        {
            public CertificationObjectDO(CertificationObjectDOBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                ID = source.ID.NotNull();
                Name = source.Name.NotNull();
                Type = source.Type.NotNull();
                TypeRef = source.TypeRef.NotNull();
            }

            public CertificationObjectDO(CertificationObjectDO source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                ID = source.ID;
                Name = source.Name;
                Type = source.Type;
                TypeRef = source.TypeRef;
            }
        }

        internal partial class CertificationObjectDOBuilder : IBuilds<CertificationObjectDO>
        {
            public ID? ID
            {
                get;
                set;
            }

            [ContractMember("Caption")]
            public string? Name
            {
                get;
                set;
            }

            public ObjectType? Type
            {
                get;
                set;
            }

            public Ref<IObjectType>? TypeRef
            {
                get;
                set;
            }

            public CertificationObjectDO Build()
            {
                return new CertificationObjectDO(this);
            }

            public static implicit operator CertificationObjectDO(CertificationObjectDOBuilder builder)
            {
                return builder.Build();
            }

            public CertificationObjectDOBuilder(CertificationObjectDO? source = null): base()
            {
                if (source != null)
                {
                    ID = source.ID;
                    Name = source.Name;
                    Type = source.Type;
                    TypeRef = source.TypeRef;
                }
            }

            public CertificationObjectDOBuilder(): base()
            {
            }
        }

        [Builder(typeof(StatusDOBuilder))]
        partial class StatusDO
        {
            public StatusDO(StatusDOBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Name = source.Name.NotNull();
            }

            public StatusDO(StatusDO source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Name = source.Name;
            }
        }

        internal partial class StatusDOBuilder : IBuilds<StatusDO>
        {
            public string? Name
            {
                get;
                set;
            }

            public StatusDO Build()
            {
                return new StatusDO(this);
            }

            public static implicit operator StatusDO(StatusDOBuilder builder)
            {
                return builder.Build();
            }

            public StatusDOBuilder(StatusDO? source = null): base()
            {
                if (source != null)
                {
                    Name = source.Name;
                }
            }

            public StatusDOBuilder(): base()
            {
            }
        }

        [Builder(typeof(UpdateServicesBuilder))]
        partial class UpdateServices
        {
            public UpdateServices(UpdateServicesBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Status = source.Status.ToImmutable();
            }

            public UpdateServices(UpdateServices source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Status = source.Status;
            }
        }

        internal partial class UpdateServicesBuilder : IBuilds<UpdateServices>
        {
            public KeyedLookup<ID, StatusDO> Status
            {
                get;
            }

            = new KeyedLookup<ID, StatusDO>();
            public UpdateServices Build()
            {
                return new UpdateServices(this);
            }

            public static implicit operator UpdateServices(UpdateServicesBuilder builder)
            {
                return builder.Build();
            }

            public UpdateServicesBuilder(UpdateServices? source = null): base()
            {
                if (source != null)
                {
                    Status.AddRange(source.Status);
                }
            }

            public UpdateServicesBuilder(): base()
            {
            }
        }
    }
}

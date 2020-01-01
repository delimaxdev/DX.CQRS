#nullable enable
namespace Codegen
{
    using System;
    using System.Collections.Generic;
    using DX;
    using DX.Contracts;
    using Newtonsoft.Json;
    using DX.Cqrs.Domain;
    using System;

    partial class SampleClass
    {
        [Builder(typeof(SampleDOBuilder))]
        partial class SampleDO
        {
            public SampleDO Mutate(Action<SampleDOBuilder> mutation)
            {
                SampleDOBuilder mutator = new SampleDOBuilder(this);
                mutation(mutator);
                return mutator;
            }

            public SampleDO(SampleDOBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                StringProp = source.StringProp.NotNull();
                DateTimeProp = source.DateTimeProp.NotNull();
                NullableDateTimeProp = source.NullableDateTimeProp;
                VerboseNullableProp = source.VerboseNullableProp;
                ArrayProp = source.ArrayProp.ToArray();
            }

            public SampleDO(SampleDO source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                StringProp = source.StringProp;
                DateTimeProp = source.DateTimeProp;
                NullableDateTimeProp = source.NullableDateTimeProp;
                VerboseNullableProp = source.VerboseNullableProp;
                ArrayProp = source.ArrayProp;
            }
        }

        /// <summary>
        /// Class comment.
        /// </summary>
        public partial class SampleDOBuilder : IBuilds<SampleDO>
        {
            /// <summary>
            /// Property comment.
            /// </summary>
            public string? StringProp
            {
                get;
                set;
            }

            //public string? NullableStringProp { get; }
            public DateTime? DateTimeProp
            {
                get;
                set;
            }

            public DateTime? NullableDateTimeProp
            {
                get;
                set;
            }

            public Nullable<int> VerboseNullableProp
            {
                get;
                set;
            }

            public List<SampleClass> ArrayProp
            {
                get;
            }

            = new List<SampleClass>();
            [JsonIgnore]
            public IEnumerable<SampleClass> ArrayPropItems
            {
                set
                {
                    ArrayProp.Clear();
                    ArrayProp.AddRange(value);
                }
            }

            public SampleDO Build()
            {
                return new SampleDO(this);
            }

            public static implicit operator SampleDO(SampleDOBuilder builder)
            {
                return builder.Build();
            }

            public SampleDOBuilder(SampleDO? source = null): base()
            {
                if (source != null)
                {
                    StringProp = source.StringProp;
                    DateTimeProp = source.DateTimeProp;
                    NullableDateTimeProp = source.NullableDateTimeProp;
                    VerboseNullableProp = source.VerboseNullableProp;
                    ArrayProp.AddRange(source.ArrayProp);
                }
            }

            public SampleDOBuilder(): base()
            {
            }
        }

        [Builder(typeof(SampleEventBuilder))]
        partial class SampleEvent
        {
            public SampleEvent Mutate(Action<SampleEventBuilder> mutation)
            {
                SampleEventBuilder mutator = new SampleEventBuilder(this);
                mutation(mutator);
                return mutator;
            }

            /// <summary>
            /// InterfaceStringProp doc.
            /// </summary>
            public string InterfaceStringProp
            {
                get;
            }

            public SampleEvent(SampleEventBuilder source): base(source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                InterfaceStringProp = source.InterfaceStringProp.NotNull();
            }

            public SampleEvent(SampleEvent source): base(source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                InterfaceStringProp = source.InterfaceStringProp;
            }
        }

        public partial class SampleEventBuilder : SampleDOBuilder, IBuilds<SampleEvent>
        {
            /// <summary>
            /// InterfaceStringProp doc.
            /// </summary>
            public string? InterfaceStringProp
            {
                get;
                set;
            }

            public new SampleEvent Build()
            {
                return new SampleEvent(this);
            }

            public static implicit operator SampleEvent(SampleEventBuilder builder)
            {
                return builder.Build();
            }

            public SampleEventBuilder(SampleEvent? source = null): base(source)
            {
                if (source != null)
                {
                    InterfaceStringProp = source.InterfaceStringProp;
                }
            }

            public SampleEventBuilder(SampleDO? source = null, ISampleInterface? sourceISampleInterface = null): base(source)
            {
                if (sourceISampleInterface != null)
                {
                    InterfaceStringProp = sourceISampleInterface.InterfaceStringProp;
                }
            }

            public SampleEventBuilder(): base()
            {
            }
        }

        [Builder(typeof(SampleCommandBuilder))]
        partial class SampleCommand
        {
            public SampleCommand Mutate(Action<SampleCommandBuilder> mutation)
            {
                SampleCommandBuilder mutator = new SampleCommandBuilder(this);
                mutation(mutator);
                return mutator;
            }

            /// <summary>
            /// InterfaceStringProp doc.
            /// </summary>
            public string InterfaceStringProp
            {
                get;
            }

            public SampleEvent BuildEvent()
            {
                return new SampleEventBuilder(this, this).Build();
            }

            public SampleCommand(SampleCommandBuilder source): base(source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                AdditionalProperty = source.AdditionalProperty.NotNull();
                InterfaceStringProp = source.InterfaceStringProp.NotNull();
            }

            public SampleCommand(SampleCommand source): base(source)
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                AdditionalProperty = source.AdditionalProperty;
                InterfaceStringProp = source.InterfaceStringProp;
            }
        }

        public partial class SampleCommandBuilder : SampleDOBuilder, IBuilds<SampleCommand>
        {
            public string? AdditionalProperty
            {
                get;
                set;
            }

            /// <summary>
            /// InterfaceStringProp doc.
            /// </summary>
            public string? InterfaceStringProp
            {
                get;
                set;
            }

            public new SampleCommand Build()
            {
                return new SampleCommand(this);
            }

            public static implicit operator SampleCommand(SampleCommandBuilder builder)
            {
                return builder.Build();
            }

            public SampleCommandBuilder(SampleCommand? source = null): base(source)
            {
                if (source != null)
                {
                    AdditionalProperty = source.AdditionalProperty;
                    InterfaceStringProp = source.InterfaceStringProp;
                }
            }

            public SampleCommandBuilder(SampleDO? source = null, ISampleInterface? sourceISampleInterface = null): base(source)
            {
                if (sourceISampleInterface != null)
                {
                    InterfaceStringProp = sourceISampleInterface.InterfaceStringProp;
                }
            }

            public SampleCommandBuilder(): base()
            {
            }
        }
    }
}
namespace Serialization
{
    using System;
    using System.Collections.Generic;
    using DX;
    using DX.Contracts;
    using Newtonsoft.Json;
    using DX.Contracts;
    using DX.Contracts.Domain;
    using DX.Testing;
    using MongoDB.Bson;
    using System;
    using System.Linq;

    partial class SerializationFeature
    {
        [Builder(typeof(UpdateCertificationObjectsBuilder))]
        partial class UpdateCertificationObjects
        {
            public UpdateCertificationObjects Mutate(Action<UpdateCertificationObjectsBuilder> mutation)
            {
                UpdateCertificationObjectsBuilder mutator = new UpdateCertificationObjectsBuilder(this);
                mutation(mutator);
                return mutator;
            }

            public UpdateCertificationObjects(UpdateCertificationObjectsBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Objects = source.Objects.ToArray();
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
            public List<CertificationObjectDO> Objects
            {
                get;
            }

            = new List<CertificationObjectDO>();
            [JsonIgnore]
            public IEnumerable<CertificationObjectDO> ObjectsItems
            {
                set
                {
                    Objects.Clear();
                    Objects.AddRange(value);
                }
            }

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
            public CertificationObjectDO Mutate(Action<CertificationObjectDOBuilder> mutation)
            {
                CertificationObjectDOBuilder mutator = new CertificationObjectDOBuilder(this);
                mutation(mutator);
                return mutator;
            }

            public CertificationObjectDO(CertificationObjectDOBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                ID = source.ID.NotNull();
                Name = source.Name.NotNull();
            }

            public CertificationObjectDO(CertificationObjectDO source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                ID = source.ID;
                Name = source.Name;
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
                }
            }

            public CertificationObjectDOBuilder(): base()
            {
            }
        }
    }
}

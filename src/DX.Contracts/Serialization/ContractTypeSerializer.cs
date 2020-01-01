using DX.Cqrs.Commons;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace DX.Contracts.Serialization {
    public class ContractTypeSerializer {
        private readonly JsonSerializer _serializer;

        public ContractTypeSerializer(SerializationTypeRegistry typeRegistry, ContractTypeSerializerOptions options) {
            Check.NotNull(typeRegistry, nameof(typeRegistry));
            Check.NotNull(options, nameof(options));

            JsonSerializerSettings s = options.CloneJsonSettings();
            s.TypeNameHandling = TypeNameHandling.None;
            s.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
            s.ContractResolver = new ContractTypeContractResolver(typeRegistry);

            options.Serializers.ConfigureJsonSettings(s);

            _serializer = JsonSerializer.Create(s);
        }

        public void Serialize<T>(JsonWriter writer, T value)
            => Serialize(writer, typeof(T), value);

        public void Serialize(JsonWriter writer, Type objectType, object? value)
            => _serializer.Serialize(writer, value, objectType);

        public T Deserialize<T>(JsonReader reader)
            => (T)Deserialize(reader, typeof(T));

        public object Deserialize(JsonReader reader, Type objectType)
            => _serializer.Deserialize(reader, objectType);

        public void EnableContractTypeResolution(JsonSerializerSettings settings)
            => new ContractTypeResolverDecorator(this).Decorate(settings);

        private static bool IsContractType(Type type) =>
            type.GetCustomAttribute<ContractAttribute>() != null;


        private class ContractTypeResolverDecorator : ContractResolverDecorator {
            private readonly ContractTypeSerializer _serializer;

            public ContractTypeResolverDecorator(ContractTypeSerializer serializer)
                => _serializer = serializer;

            public override JsonContract ResolveContract(Type type)
                => IsContractType(type) ?
                    new JsonObjectContract(type) { Converter = new ContractTypeConverter(_serializer) } :
                    base.ResolveContract(type);
        }

        private class ContractTypeConverter : JsonConverter {
            private readonly ContractTypeSerializer _serializer;

            public ContractTypeConverter(ContractTypeSerializer serializer)
                => _serializer = serializer;

            public override bool CanConvert(Type objectType)
                => true;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                => _serializer.Deserialize(reader, objectType);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => _serializer.Serialize(writer, value?.GetType() ?? typeof(object), value);
        }

        private class ContractTypeContractResolver : DefaultContractResolver {
            private static readonly StringEnumConverter __enumConverter = new StringEnumConverter(
                new DefaultNamingStrategy(),
                allowIntegerValues: false);
            private static readonly ConcurrentDictionary<Type, JsonContract> __contractCache = 
                new ConcurrentDictionary<Type, JsonContract>();
            private readonly SerializationTypeRegistry _types;

            public ContractTypeContractResolver(SerializationTypeRegistry types)
                => _types = types;

            protected override JsonContract CreateContract(Type objectType) {
                return __contractCache.GetOrAdd(objectType, CreateContractCore);
            }

            private JsonContract CreateContractCore(Type objectType) {
                Type t = Nullable.GetUnderlyingType(objectType) ?? objectType;

                if (!IsKnownType(t) && !IsCollectionType(t) && !IsContractType(t) && !IsBuilder(t)) {
                    throw new ContractTypeSerializationException($"Cannot serialize the type {t.Name} because " +
                        $"it does not have the ContractAttribute. Make sure that all your contract classes (and all " +
                        $"the types they reference) have the ContractAttribute.");
                }

                if (t.IsEnum) {
                    return new JsonStringContract(t) { Converter = __enumConverter };
                }

                JsonConverter? inheritanceConverter = ContractType.IsPolymorphicContract(objectType) ?
                    new PolymorphicJsonConverter(_types) :
                    null;

                BuilderAttribute? builderAttr = objectType.GetCustomAttribute<BuilderAttribute>(inherit: false);
                if (builderAttr != null) {
                    JsonConverter builderConverter = BuilderJsonConverter.Create(objectType, builderAttr.BuilderType);
                    return new JsonObjectContract(objectType) {
                        Converter = inheritanceConverter != null ?
                            new CompositeJsonConverter(objectType, inheritanceConverter, builderConverter) :
                            builderConverter
                    };
                }

                JsonContract defaultContract = base.CreateContract(objectType);
                if (inheritanceConverter != null) {
                    Expect.That(defaultContract.Converter == null);
                    defaultContract.Converter = inheritanceConverter;
                }

                return defaultContract;
            }

            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization) {
                JsonProperty property = base.CreateProperty(member, memberSerialization);
                property.PropertyName = ContractType.GetMemberName(member);
                return property;
            }

            private static bool IsKnownType(Type t) {
                return t.IsPrimitive
                    || t.Equals(typeof(string))
                    || t.Equals(typeof(decimal))
                    || t.Equals(typeof(DateTime))
                    || t.Equals(typeof(TimeSpan))
                    || t.Equals(typeof(Guid))
                    || t.Equals(typeof(IPAddress))
                    || t.Equals(typeof(IPEndPoint))
                    || t.Equals(typeof(Uri))
                    || t.Equals(typeof(ITuple));
            }

            private static bool IsCollectionType(Type type)
                => type.GetInterface("IEnumerable") != null;

            private static bool IsBuilder(Type type)
                => ReflectionUtils.GetGenericInterfaceImplementations(type, typeof(IBuilds<>)).Any();
        }
    }

    [Serializable]
    public class ContractTypeSerializationException : Exception {
        public ContractTypeSerializationException() { }
        public ContractTypeSerializationException(string message) : base(message) { }
        public ContractTypeSerializationException(string message, Exception inner) : base(message, inner) { }
        protected ContractTypeSerializationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

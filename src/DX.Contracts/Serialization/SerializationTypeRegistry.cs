using DX.Cqrs.Commons;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace DX.Contracts.Serialization {
    public class SerializationTypeRegistry {
        public static readonly SerializationTypeRegistry Default = new SerializationTypeRegistry();

        private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();
        private readonly List<Assembly> _knownAssemblies = new List<Assembly>();

        private readonly ConcurrentDictionary<Type, SerializationTypeInfo> _infoByType
            = new ConcurrentDictionary<Type, SerializationTypeInfo>();
        private readonly ConcurrentDictionary<string, SerializationTypeInfo> _infoByDiscriminator
            = new ConcurrentDictionary<string, SerializationTypeInfo>();

        public SerializationTypeRegistry() { }

        /// <summary>
        /// JUST for UNIT TESTING!
        /// </summary>
        internal SerializationTypeRegistry(IEnumerable<Type> knownTypes) {
            Process(knownTypes);
        }

        public bool TryGetInfo(Type type, out SerializationTypeInfo info) {

            _cacheLock.EnterReadLock();
            try {
                if (_infoByType.TryGetValue(type, out info)) {
                    return true;
                }
            } finally {
                _cacheLock.ExitReadLock();
            }

            _cacheLock.EnterWriteLock();
            try {
                EnsureAssembly(type.Assembly);
            } finally {
                _cacheLock.ExitWriteLock();
            }

            if (_infoByType.TryGetValue(type, out info)) {
                return true;
            }

            return false;
        }

        public SerializationTypeInfo GetInfo(Type type) {
            if (TryGetInfo(type, out SerializationTypeInfo result))
                return result;

            Check.Requires(
                GetModuleCode(type.Assembly) != null, nameof(type),
                "The containing assmbly does not have the ContractAssemblyAttribute.");

            Check.Requires(type.GetCustomAttribute<ContractAttribute>() != null, nameof(type),
                "The requested type does not have the ContractAttribute.");

            throw new ArgumentException(
                "The serialization configuration of the requested type is invalid or incomplete.",
                nameof(type));
        }

        public SerializationTypeInfo GetInfo(string discriminator) {
            SerializationTypeInfo result;

            _cacheLock.EnterReadLock();
            try {
                if (_infoByDiscriminator.TryGetValue(discriminator, out result)) {
                    return result;
                }
            } finally {
                _cacheLock.ExitReadLock();
            }

            _cacheLock.EnterWriteLock();
            try {
                EnsureModule(SerializationTypeName.Parse(discriminator).ModuleCode);
            } finally {
                _cacheLock.ExitWriteLock();
            }

            if (_infoByDiscriminator.TryGetValue(discriminator, out result)) {
                return result;
            }

            throw new ArgumentException(
                $"No valid serialization configuration found for discriminator '{discriminator}'.",
                nameof(discriminator));
        }

        private void EnsureAssembly(Assembly assembly) {
            if (!_knownAssemblies.Contains(assembly)) {
                Process(assembly);
            }
        }

        private void EnsureModule(string moduleCode) {
            IEnumerable<Assembly> modulesAssemblies = AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(a => GetModuleCode(a) == moduleCode);

            foreach (Assembly assembly in modulesAssemblies) {
                EnsureAssembly(assembly);
            }
        }

        private void Process(Assembly assembly) {
            // GetTypes() also returns nested an private types, so we do not need to
            // explicitly handles this case
            Process(assembly.GetTypes());
        }

        private void Process(IEnumerable<Type> types) {
            foreach (Type type in types) {
                Process(type);
            }
        }

        private SerializationTypeInfo? Process(Type type) {
            if (_infoByType.TryGetValue(type, out SerializationTypeInfo result)) {
                return result;
            }

            ContractAttribute? attribute = type.GetCustomAttribute<ContractAttribute>(inherit: false);
            if (attribute != null) {
                string? moduleCode = GetModuleCode(type.Assembly);

                if (moduleCode != null) {
                    string typeName = attribute.Name ?? RemoveGenericBackticks(type.Name);
                    string? containerName = GetContractContainerName(type);

                    var name = containerName != null ?
                        new SerializationTypeName(moduleCode, containerName, typeName) :
                        new SerializationTypeName(moduleCode, typeName);

                    ContractTypeInfo contractInfo = new ContractTypeInfo(type, name);

                    // HACK: This seems kind of hacky... Maybe rethink...
                    if (!type.IsGenericTypeDefinition) {
                        Expect.That(_infoByDiscriminator.TryAdd(contractInfo.Discriminator, contractInfo));
                    }

                    result = contractInfo;
                } else {
                    result = new SerializationTypeInfo(type);
                }

                Expect.That(_infoByType.TryAdd(result.Type, result));

                bool hasBaseType = !type.IsValueType
                    && type.BaseType != typeof(object)
                    && type.BaseType != null;

                IEnumerable<Type> baseType = hasBaseType ? new[] { type.BaseType! } : Enumerable.Empty<Type>();

                foreach (Type t in baseType.Concat(ReflectionUtils.GetAllInterfaces(type))) {
                    SerializationTypeInfo? baseInfo = Process(t);
                    if (baseInfo != null) {
                        if (!type.IsGenericTypeDefinition) {
                            baseInfo.Subclasses.Add(result);
                        }
                    }
                }
            }

            return result;
        }

        private static string? GetContractContainerName(Type t) {
            //Check.Requires(
            //    t.DeclaringType != null, nameof(t),
            //    "The given type is not contained in a type that has a ContractContainerAttribute.");

            if (t.DeclaringType == null)
                return null;
            
            t = t.DeclaringType;

            ContractContainerAttribute? attribute = t.GetCustomAttribute<ContractContainerAttribute>(inherit: false);
            return attribute != null ?
                attribute.Name ?? GetNameFromType(t) :
                GetContractContainerName(t);
        }

        private static string? GetModuleCode(Assembly assembly)
            => assembly.GetCustomAttribute<ContractAssemblyAttribute>()?.ModuleCode;

        private static string RemoveGenericBackticks(string typeName) {
            int pos = typeName.IndexOf('`');
            return pos > 0 ?
                 typeName.Remove(pos) :
                 typeName;
        }

        private static string GetNameFromType(Type t) {
            string name = RemoveGenericBackticks(t.Name);
            return t.IsInterface && name.StartsWith('I') ?
                name.Substring(1) :
                name;
        }
    }

    public sealed class SerializationTypeName {
        private static readonly Regex __regex = new Regex(@"^(\w+):([\w.]+)$");

        public string ModuleCode { get; }

        public string Name { get; }

        public string QualifiedName => $"{ModuleCode}:{Name}";

        public SerializationTypeName(string moduleCode, string name) {
            ModuleCode = moduleCode;
            Name = Check.NotEmpty(name, nameof(name));
        }

        public SerializationTypeName(string? moduleCode, string containerName, string typeName)
            : this(moduleCode, $"{containerName}.{typeName}") { }

        public static SerializationTypeName Parse(string qualifiedName) {
            Match m = __regex.Match(qualifiedName);
            Check.Requires(m.Success, nameof(qualifiedName), "The string '{0}' is not a valid serialization name.", qualifiedName);
            return new SerializationTypeName(m.Groups[1].Value, m.Groups[2].Value);
        }

        public override bool Equals(object obj)
            => obj is SerializationTypeName other &&
                ModuleCode == other.ModuleCode &&
                Name == other.Name &&
                QualifiedName == other.QualifiedName;

        public override int GetHashCode()
            => HashCode.Combine(ModuleCode, Name, QualifiedName);

        public override string ToString() => QualifiedName;
    }

    public class SerializationTypeInfo {
        public Type Type { get; }

        public List<SerializationTypeInfo> Subclasses { get; } = new List<SerializationTypeInfo>();

        public SerializationTypeInfo(Type type) {
            Type = Check.NotNull(type, nameof(type));
        }
    }

    public class ContractTypeInfo : SerializationTypeInfo {
        public SerializationTypeName Name { get; }

        public string Discriminator { get; }

        public ContractTypeInfo(Type type, SerializationTypeName name) : base(type) {
            Name = Check.NotNull(name, nameof(name));
            Discriminator = name.QualifiedName;
        }

        public override string ToString() => $"{Name} ({Type.Name})";
    }
}

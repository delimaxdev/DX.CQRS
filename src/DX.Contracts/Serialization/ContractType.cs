using DX.Cqrs.Commons;
using System;
using System.Linq;
using System.Reflection;

namespace DX.Contracts.Serialization {
    public static class ContractType {
        public static bool IsPolymorphicContract(Type type) {
            Check.NotNull(type, nameof(type));
            return new[] { type }
                .Concat(ReflectionUtils.GetAllBaseClasses(type))
                .Concat(ReflectionUtils.GetAllInterfaces(type))
                .SelectMany(t => t.GetCustomAttributes<ContractAttribute>(inherit: true))
                .Any(attr => attr.IsPolymorphic == true);
        }

        public static string GetMemberName(MemberInfo member) {
            Check.NotNull(member, nameof(member));

            ContractMemberAttribute? attr = member.GetCustomAttribute<ContractMemberAttribute>(inherit: true);
            if (attr != null && attr.Name != null) {
                return attr.Name;
            }

            if (member is PropertyInfo p && Ref.IsRefType(p.PropertyType)) {
                return member.Name + "ID";
            }

            return member.Name;
        }
    }
}
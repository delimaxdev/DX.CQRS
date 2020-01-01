using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DX.Cqrs.Commons {
    internal static class ReflectionUtils {
        public static bool IsGenericType(Type type, Type genericTypeDefinition) {
            Check.NotNull(type, nameof(type));
            Check.NotNull(genericTypeDefinition, nameof(genericTypeDefinition));

            return type.IsGenericType 
                && Equals(type.GetGenericTypeDefinition(), genericTypeDefinition);
        }

        public static PropertyInfo GetPropertyInfo(LambdaExpression expression) {
            if (expression.Body is MemberExpression memberExp &&
                memberExp.Expression is ParameterExpression &&
                memberExp.Member is PropertyInfo p
            ) {
                return p;
            }

            throw new ArgumentException("The given expression is not a simple property expression like x => x.Name.");
        }

        public static IReadOnlyCollection<Type> GetAllInterfaces(Type type) {
            List<Type> result = new List<Type>(type.GetInterfaces());

            for (int i = 0; i < result.Count; i++) {
                foreach (Type baseInterface in result[i].GetInterfaces()) {
                    if (!result.Contains(baseInterface)) {
                        result.Add(baseInterface);
                    }
                }
            }

            return result;
        }

        public static IEnumerable<Type> GetAllBaseClasses(Type type) {
            Type? t = type.BaseType;
            while (t != null && t != typeof(object)) {
                yield return t;
                t = t.BaseType;
            }
        }

        public static InterfaceImplementation[] GetGenericInterfaceImplementations(Type type, Type interfaceType) {
            Check.NotNull(type, nameof(type));
            Check.NotNull(interfaceType, nameof(interfaceType));
            Check.Requires(interfaceType.IsGenericType);

            return GetAllInterfaces(type)
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition().Equals(interfaceType))
                .Select(i => new InterfaceImplementation(i.GenericTypeArguments))
                .ToArray();
        }

        public class InterfaceImplementation {
            public Type[] GenericTypeArguments { get; }

            internal InterfaceImplementation(Type[] genericTypeArguments)
                => GenericTypeArguments = genericTypeArguments;
        }
    }
}

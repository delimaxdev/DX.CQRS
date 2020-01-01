using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Serialization;

namespace DX.Cqrs.Commons
{
    public class XmlSerializerConfigurator {
        private readonly XmlAttributeOverrides _overrides = new XmlAttributeOverrides();

        public void MapType<T>(Action<TypeMapper<T>> mapAction)
            => mapAction(new TypeMapper<T>(_overrides));

        public XmlSerializer CreateSerializer(Type type)
            => new XmlSerializer(type, _overrides);

        public class TypeMapper<T> {
            private readonly XmlAttributeOverrides _overrides;

            internal TypeMapper(XmlAttributeOverrides overrides)
                => _overrides = overrides;

            public void XmlName<TValue>(Expression<Func<T, TValue>> propertySelector, string xmlName)
                => AddAttribute(propertySelector, new XmlAttributes { XmlElements = { new XmlElementAttribute(xmlName) } });

            public OverrideMapper<T> XmlName<TValue>(Expression<Func<T, TValue>> propertySelector) {
                XmlIgnore(propertySelector);
                return new OverrideMapper<T>(_overrides, ReflectionUtils.GetPropertyInfo(propertySelector));
            }

            public void XmlIgnore<TValue>(Expression<Func<T, TValue>> propertySelector)
                => AddAttribute(propertySelector, new XmlAttributes { XmlIgnore = true });

            private void AddAttribute(LambdaExpression propertySelector, XmlAttributes attributes) {
                PropertyInfo member = ReflectionUtils.GetPropertyInfo(propertySelector);
                
                // We MUST use the type that declares the property here, otherwise the XmlSerializer
                // ignores our override!
                _overrides.Add(member.DeclaringType, member.Name, attributes);
            }
        }

        public class OverrideMapper<T> {
            private readonly XmlAttributeOverrides _overrides;
            private readonly string _memberName;

            internal OverrideMapper(XmlAttributeOverrides overrides, PropertyInfo member)
                => (_overrides, _memberName) = (overrides, member.Name);

            public OverrideMapper<T> ForOverride<TSubclass>(string xmlName) where TSubclass : T {
                _overrides.Add(
                    typeof(TSubclass),
                    _memberName, new XmlAttributes {
                        XmlElements = { new XmlElementAttribute(xmlName) }
                    });

                return this;
            }
        }
    }
}
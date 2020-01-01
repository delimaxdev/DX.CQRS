using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DX.Cqrs.Commons
{
    internal static class ObjectUtils {

        public static T ShallowCopyTo<T>(object source, T target) {
            Check.NotNull(source, nameof(source));
            Check.NotNull(target, nameof(target));

            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            IEnumerable<PropertyInfo> sourceProperties = source.GetType().GetProperties(flags);
            IEnumerable<PropertyInfo> targetProperties = target.GetType().GetProperties(flags);

            IEnumerable<(PropertyInfo TargetProperty, PropertyInfo SourceProperty)> matching = targetProperties
                .Join(sourceProperties, p => p.Name, q => q.Name, (p, q) => (p, q));

            foreach (var pair in matching) {
                (PropertyInfo sp, PropertyInfo tp) = (pair.SourceProperty, pair.TargetProperty); 

                object sourceValue = sp.GetValue(source);
                object targetValue = tp.GetValue(target);

                if (targetValue != null && sourceValue is IEnumerable items) {
                    MethodInfo add = tp.PropertyType.GetMethod("Add");

                    if (add != null) {
                        MethodInfo clear = tp.PropertyType.GetMethod("Clear");
                        if (clear != null) {
                            clear.Invoke(targetValue, new object[0]);
                        }

                        foreach (object item in items) {
                            add.Invoke(targetValue, new[] { item });
                        }

                        continue;
                    }
                }

                if (tp.PropertyType.IsAssignableFrom(sp.PropertyType) && tp.CanWrite && sp.CanRead) {
                    tp.SetValue(target, sp.GetValue(source));
                }
            }

            return target;
        }
    }
}

using DX.Cqrs.Commons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DX.Contracts {

    public class Ref<T> : IRefImplementation<T> where T : IHasID<IIdentifier> {
        private readonly IIdentifier _id;

        IIdentifier IRefImplementation<T>.ID => _id;

        internal Ref(IIdentifier id) {
            Check.NotNull(id, nameof(id));
            _id = id;
        }

        public Ref<U> Cast<U>() where U : T
            => new Ref<U>(_id);

        public override bool Equals(object? other) {
            if (EqualsImplementaiton(other))
                return true;

            if (other is IRefImplementation<IHasID<IIdentifier>> covariantOther)
                return covariantOther.EqualsImplementation(this);

            return false;
        }

        public override int GetHashCode()
            => _id.GetHashCode();

        public override string ToString()
            => $"<{typeof(T).Name} {_id}>";

        public static bool operator ==(Ref<T>? left, Ref<T>? right)
            => Equals(left, right);

        public static bool operator !=(Ref<T>? left, Ref<T>? right)
            => !Equals(left, right);

        public static implicit operator Ref<T>(T instance) {
            Check.NotNull(instance, nameof(instance));
            return new Ref<T>(instance.ID);
        }

        Ref<U> IRefImplementation<T>.CovariantCast<U>()
            => new Ref<U>(_id);

        bool IRefImplementation<T>.EqualsImplementation(object other)
            => EqualsImplementaiton(other);

        private bool EqualsImplementaiton(object other)
            => other is IRefImplementation<T> i && Equals(i.ID, _id);
    }

    public static class Ref {
        private static readonly string RefTypeName = typeof(Ref<>).Name;

        public static bool IsRefType(Type type) {
            Check.NotNull(type, nameof(type));

            // We check the 'Name' first for performance reasons
            return type.Name == RefTypeName 
                && type.GetGenericTypeDefinition() == typeof(Ref<>);
        }

        public static Type GetTargetType(Type refType) {
            Check.Requires(ReflectionUtils.IsGenericType(refType, typeof(Ref<>)), nameof(refType));
            return refType.GenericTypeArguments.Single();
        }

        public static Type GetIdentifierType(Type targetType) {
            Check.NotNull(targetType, nameof(targetType));

            if (ReflectionUtils.IsGenericType(targetType, typeof(IHasID<>)))
                return targetType.GenericTypeArguments.Single();
            
            return ReflectionUtils
                .GetGenericInterfaceImplementations(targetType, typeof(IHasID<>))
                .Single()
                .GenericTypeArguments
                .Single();
        }

        public static void ExtractIdentifier<T, TID>(Ref<T> r, out TID id)
            where TID : IIdentifier
            where T : IHasID<TID>, IHasID<IIdentifier> {

            Check.NotNull(r, nameof(r));
            IRefImplementation<T> i = r;
            id = (TID)i.ID;
        }

        public static Ref<T> FromIdentifier<T, TID>(TID id)
            where TID : IIdentifier
            where T : IHasID<TID>, IHasID<IIdentifier> {

            Check.NotNull(id, nameof(id));
            return new Ref<T>(id);
        }

        public static Ref<U> Cast<U>(this ICovarantRef<U> r) where U : IHasID<IIdentifier> {
            Check.NotNull(r, nameof(r));
            var i = (IRefImplementation<U>)r;
            return i.CovariantCast<U>();
        }
    }

    public interface ICovarantRef<out T> where T : IHasID<IIdentifier> { }

    internal interface IRefImplementation<out T> : ICovarantRef<T> where T : IHasID<IIdentifier> {
        IIdentifier ID { get; }

        Ref<U> CovariantCast<U>() where U : IHasID<IIdentifier>;

        bool EqualsImplementation(object other);
    }
}
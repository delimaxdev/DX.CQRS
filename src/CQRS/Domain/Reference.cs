using DX.Contracts;
using DX.Cqrs.Domain.Core;
using System;
using System.Collections.Generic;

namespace DX.Cqrs.Domain {
    public class Reference<T> where T : AggregateRoot {
        private readonly ID _id;

        public Reference(ID id) {
            _id = id;
        }

        public override bool Equals(object obj) {
            return obj is Reference<T> reference &&
                   _id.Equals(reference._id);
        }

        public override int GetHashCode()
            => _id.GetHashCode();

        public static bool operator ==(Reference<T> left, Reference<T> right) {
            return EqualityComparer<Reference<T>>.Default.Equals(left, right);
        }

        public static bool operator !=(Reference<T> left, Reference<T> right) {
            return !(left == right);
        }

        public static implicit operator Reference<T>(T aggregateRoot) {
            return new Reference<T>(((IPersistable)aggregateRoot).ID);
        }

        public static implicit operator ID(Reference<T> r) {
            return r._id;
        }

        public static explicit operator Reference<T>(ID id) {
            return new Reference<T>(id);
        }
    }
}
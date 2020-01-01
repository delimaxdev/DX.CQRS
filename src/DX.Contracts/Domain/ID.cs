using System;
using System.ComponentModel;
using System.Globalization;

namespace DX.Contracts {
    [TypeConverter(typeof(IDTypeConverter))]
    public class ID : IIdentifier, IEquatable<ID> {
        private readonly Guid _value;

        private ID(Guid value) {
            Check.Requires(value != Guid.Empty, nameof(value));
            _value = value;
        }

        public Ref<T> ToRef<T>() where T : IHasID<ID>
            => Ref.FromIdentifier<T, ID>(this);

        public static ID Parse(string input) {
            Check.NotEmpty(input, nameof(input));
            Guid value = Guid.Parse(input);
            return ID.FromGuid(value);
        }

        public static ID FromGuid(Guid value) {
            return new ID(value);
        }

        public static Guid ToGuid(ID id) {
            Check.NotNull(id, nameof(id));
            return id._value;
        }

        public static ID FromRef<T>(Ref<T> r) where T : IHasID<ID> {
            Check.NotNull(r, nameof(r));
            Ref.ExtractIdentifier(r, out ID id);
            return id;
        }

        public static ID NewID()
            => ID.FromGuid(Guid.NewGuid());

        public bool Equals(ID? other) 
            => !ReferenceEquals(other, null) && _value == other!._value;

        public override bool Equals(object other)
            => Equals(other as ID);

        public override int GetHashCode()
            => _value.GetHashCode();

        public override string ToString()
            => _value.ToString();

        public static bool operator ==(ID? id1, ID? id2) 
            => Equals(id1, id2);

        public static bool operator !=(ID? id1, ID? id2)
            => !Equals(id1, id2);
    }

    // TODO: Make this a bit nicer...
    public class IDTypeConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return typeof(String).Equals(sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            return ID.FromGuid(Guid.Parse((string)value));
        }
    }
}
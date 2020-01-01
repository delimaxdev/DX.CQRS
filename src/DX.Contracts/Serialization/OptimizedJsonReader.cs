using Newtonsoft.Json;

namespace DX.Contracts.Serialization {
    public abstract class OptimizedJsonReader : JsonReader {
        public abstract bool ReadDiscriminatorOrNull(string propertyName, out string? value);
    }
}
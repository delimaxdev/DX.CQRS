using Newtonsoft.Json;

namespace DX.Contracts.Serialization
{
    public abstract class OptimizedJsonWriter : JsonWriter {
        public abstract void InjectDiscriminator(string name, string value);
    }
}
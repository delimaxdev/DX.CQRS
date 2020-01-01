using Newtonsoft.Json;
using System;

namespace DX.Contracts.Serialization
{
    public abstract class RecursionSafeJsonConverter : JsonConverter {
        [ThreadStatic] private static bool _isReading;
        [ThreadStatic] private static bool _isWriting;

        public override bool CanWrite {
            get {
                if (_isWriting) {
                    _isWriting = false;
                    return false;
                }
                return true;
            }
        }

        public override bool CanRead {
            get {
                if (_isReading) {
                    _isReading = false;
                    return false;
                }
                return true;
            }
        }

        public sealed override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            try {
                _isWriting = true;
                WriteJsonCore(writer, value, serializer);
            } finally {
                _isWriting = false;
            }
        }

        public sealed override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            try {
                _isReading = true;
                return ReadJsonCore(reader, objectType, existingValue, serializer);
            } finally {
                _isReading = false;
            }
        }

        protected abstract object ReadJsonCore(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer);

        protected abstract void WriteJsonCore(JsonWriter writer, object value, JsonSerializer serializer);
    }
}
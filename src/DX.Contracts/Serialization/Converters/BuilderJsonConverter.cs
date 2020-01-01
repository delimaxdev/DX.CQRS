using Newtonsoft.Json;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace DX.Contracts.Serialization {
    public class BuilderJsonConverter {
        public static JsonConverter Create(Type messageType, Type builderType) =>
            (JsonConverter)Activator.CreateInstance(
                typeof(Implementation<,>).MakeGenericType(
                    Check.NotNull(messageType, nameof(messageType)),
                    Check.NotNull(builderType, nameof(builderType))));

        private class Implementation<TMessage, TBuilder> : JsonConverter
            where TMessage : class
            where TBuilder : IBuilds<TMessage> {

            private static readonly Func<TMessage, TBuilder> __builderFactory = CreateFastBuilderFactory();

            public override bool CanConvert(Type objectType) => true;

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
                TBuilder b = (TBuilder)serializer.Deserialize(reader, typeof(TBuilder));
                return b.Build();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
                TBuilder builder = __builderFactory((TMessage)value);
                serializer.Serialize(writer, builder);
            }

            private static Func<TMessage, TBuilder> CreateFastBuilderFactory() {
                ConstructorInfo builderConstructor = typeof(TBuilder).GetConstructor(new[] { typeof(TMessage) });
                ParameterExpression p = Expression.Parameter(typeof(TMessage), "m");
                return (Func<TMessage, TBuilder>)Expression.Lambda(Expression.New(builderConstructor, p), p).Compile();
            }
        }
    }
}

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace DX.Contracts.Serialization {
    public abstract class ContractResolverDecorator : IContractResolver {
        private static readonly DefaultContractResolver __defaultRessolver = new DefaultContractResolver();
        private IContractResolver? _nextResolver;
        private bool _alreadyDecorating = false;

        private IContractResolver NextResolver {
            get => _nextResolver ?? __defaultRessolver;
            set => _nextResolver = value;
        }

        public virtual JsonContract ResolveContract(Type type)
            => NextResolver.ResolveContract(type);

        public void Decorate(JsonSerializerSettings settings) {
            Check.NotNull(settings, nameof(settings));
            Decorate(settings.ContractResolver);
            settings.ContractResolver = this;
        }

        public void Decorate(IContractResolver actual) {
            Check.Requires<InvalidOperationException>(!_alreadyDecorating);
            NextResolver = actual;
        }
    }
}
using DX.Contracts.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace DX.Web.Serialization {
    using Microsoft.Extensions.Options;

    public class MvcJsonOptionsSetup : IPostConfigureOptions<MvcNewtonsoftJsonOptions> {
        private readonly SerializerManager _manager;

        public MvcJsonOptionsSetup(SerializerManager manager) 
            => _manager = Check.NotNull(manager, nameof(manager));

        public void PostConfigure(string name, MvcNewtonsoftJsonOptions options) {
            Check.NotNull(options, nameof(options));

            if (name == Options.DefaultName) {
                _manager.ConfigureJsonSerializerSettings(options.SerializerSettings);
            }
        }
    }
}

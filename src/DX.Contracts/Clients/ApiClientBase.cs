using DX.Contracts.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DX.Contracts.Clients {
    public abstract class ApiClientBase : IDisposable {
        private readonly Lazy<MediaTypeFormatter> _jsonFormatter;

        protected SerializerSetup SerializerSetups { get; } = new SerializerSetup();

        protected HttpClient Client { get; private set; }

        protected string RequestPathPrefix { get; }

        protected MediaTypeFormatter JsonFormatter
            => _jsonFormatter.Value;

        protected ApiClientBase(Uri baseAddress, string? requestPathPrefix = null) : this(requestPathPrefix) {
            Check.NotNull(baseAddress, nameof(baseAddress));
            SetClient(new HttpClient { BaseAddress = baseAddress });
        }

        protected ApiClientBase(HttpClient client, string? requestPathPrefix = null) : this(requestPathPrefix) {
            Check.NotNull(client, nameof(client));
            SetClient(client);
        }

        private ApiClientBase(string? requestPathPrefix) {
            _jsonFormatter = new Lazy<MediaTypeFormatter>(SerializerSetups.CreateFormatter);
            RequestPathPrefix = requestPathPrefix ?? String.Empty;
        }

        public void Dispose() {
            Client.Dispose();
        }

        protected async Task<TResult> PostAsync<T, TResult>(string requestUri, T value) {
            using HttpResponseMessage response = await PostAsyncCore(requestUri, value);
            return await response.Content.ReadAsAsync<TResult>(new[] { JsonFormatter });
        }

        protected async Task PostAsync<T>(string requestUri, T value) {
            using HttpResponseMessage response = await PostAsyncCore(requestUri, value);
        }

        protected async Task<HttpResponseMessage> PostAsyncCore<T>(string requestUri, T value) {
            HttpResponseMessage response = await Client.PostAsync(PrefixUri(requestUri), value, JsonFormatter);
            if (!response.IsSuccessStatusCode && Debugger.IsAttached)
                Debugger.Break();

            return response.EnsureSuccessStatusCode();
        }

        protected Uri PrefixUri(string relativeUri)
            => new Uri(RequestPathPrefix + relativeUri, UriKind.Relative);

        private void SetClient(HttpClient client) {
            Client = client;
            Client.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        protected class SerializerSetup {
            private readonly List<IConfigureSerializers> _setups = new List<IConfigureSerializers>();

            public SerializerSetup()
                => _setups.Add(new DefaultSerializerSetup());

            public void Add(IConfigureSerializers setup)
                => _setups.Add(Check.NotNull(setup, nameof(setup)));

            internal MediaTypeFormatter CreateFormatter() {
                JsonMediaTypeFormatter formatter = new JsonMediaTypeFormatter();
                SerializerManager factory = new SerializerManager(_setups, SerializationTypeRegistry.Default);
                factory.ConfigureJsonSerializerSettings(formatter.SerializerSettings);
                return formatter;
            }
        }
    }
}

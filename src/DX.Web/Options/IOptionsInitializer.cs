using System.Collections.Generic;

namespace DX.Web.Options {
    using Microsoft.Extensions.Options;

    public interface IOptionsInitializer<TOptions> where TOptions : class {
        void Initialize(TOptions options, string name);
    }

    public class OptionsInitializer<TOptions> : IOptionsInitializer<TOptions> where TOptions : class {
        private readonly IEnumerable<IConfigureOptions<TOptions>> _setups;
        private readonly IEnumerable<IPostConfigureOptions<TOptions>> _postConfigures;
        private readonly IEnumerable<IValidateOptions<TOptions>> _validations;

        public OptionsInitializer(
            IEnumerable<IConfigureOptions<TOptions>> setups,
            IEnumerable<IPostConfigureOptions<TOptions>> postConfigures,
            IEnumerable<IValidateOptions<TOptions>> validations
        ) {
            _setups = Check.NotNull(setups, nameof(setups));
            _postConfigures = Check.NotNull(postConfigures, nameof(postConfigures));
            _validations = Check.NotNull(validations, nameof(validations));
        }

        public void Initialize(TOptions options, string name) {
            foreach (var setup in _setups) {
                if (setup is IConfigureNamedOptions<TOptions> namedSetup) {
                    namedSetup.Configure(name, options);
                } else if (name == Options.DefaultName) {
                    setup.Configure(options);
                }
            }

            foreach (var post in _postConfigures) {
                post.PostConfigure(name, options);
            }

            if (_validations != null) {
                var failures = new List<string>();
                foreach (var validate in _validations) {
                    ValidateOptionsResult result = validate.Validate(name, options);
                    if (result.Failed) {
                        failures.Add(result.FailureMessage);
                    }
                }
                if (failures.Count > 0) {
                    throw new OptionsValidationException(name, typeof(TOptions), failures);
                }
            }
        }
    }
}
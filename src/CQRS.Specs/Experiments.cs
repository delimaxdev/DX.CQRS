using DX.Testing;
using Xbehave;
using Xunit.Abstractions;

namespace DX
{
    public class Experiments : Feature {
        private ITestOutputHelper _output;

        public Experiments(ITestOutputHelper output)
            => _output = output;

        [Scenario]
        public void Experiment() {
            CUSTOM["Experiment"] = () => {

            };
        }
    }
}

using DX.Cqrs.Mongo.Facade;
using Xunit;
using Xunit.Abstractions;

namespace DX.Testing {
    [Collection("BSON")]
    public class MongoFeature : BsonSerializationFeature {
        protected ITestOutputHelper Output { get; }

        internal MongoTestEnvironment Env { get; set; }
        internal MongoFacade DB { get; set; }

        public MongoFeature(ITestOutputHelper output)
            => Output = output;

        public override void Background() {
            base.Background();
            USING["a mongo DB"] = () => Env = new MongoTestEnvironment(Output);
            AND["a mongo facade"] = () => DB = Env.GetFacade();
        }
    }
}

namespace Mongo
{
    internal partial class MongoAction {
        public virtual void Execute(MongoFake.FakeStore store) { }

        public override string ToString() {
            return GetType().Name;
        }
    }
}

namespace Mongo
{
    internal class MongoActionFactory : MongoActionFluentInterface<MongoAction> {
        public static readonly MongoActionFactory Default = new MongoActionFactory();

        protected override MongoAction OnCreate(MongoAction action) {
            return action;
        }
    }
}

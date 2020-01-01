using DX.Cqrs.Commons;
using DX.Cqrs.Mongo.Facade;
using FluentAssertions;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace DX.Testing {
    public class MongoTestEnvironment : Disposable {
        private static bool __mongoClientCreated = false;
        private static IMongoClient __client;
        private static MongoRunner __mongoRunner = new MongoRunner();

        private readonly ITestOutputHelper _output;
        private readonly Lazy<(string Name, IMongoDatabase DB)> _db;
        private readonly List<string> _createdDatabases = new List<string>();

        public IMongoDatabase DB => _db.Value.DB;

        public string DatabaseName => _db.Value.Name;

        public string ConnectionString => __mongoRunner.ConnectionString;

        public IMongoClient Client {
            get {
                if (!__mongoClientCreated) {
                    __mongoClientCreated = true;
                    CreateMongoClient();
                }

                __client.Should().NotBeNull();
                return __client;
            }
        }

        public MongoTestEnvironment(ITestOutputHelper output) {
            _output = output;
            _db = new Lazy<(string, IMongoDatabase)>(CreateTemporaryDBCore);
        }

        public string CreateTemporyDB() {
            return CreateTemporaryDBCore().Item1;
        }

        internal MongoFacade GetFacade() {
            return new MongoFacade(Client, DatabaseName);
        }

        protected override void Dispose(bool disposing) {
            if (!disposing)
                return;

            foreach (string db in _createdDatabases) {
                Client.DropDatabase(db);
            }
        }

        private (string, IMongoDatabase) CreateTemporaryDBCore() {
            string name = $"TEST_{Guid.NewGuid()}";
            _createdDatabases.Add(name);
            IMongoDatabase db = Client.GetDatabase(name);
            return (name, db);
        }

        private void CreateMongoClient() {
            if (!MongoRunner.ProcessIsAlreadyRunning()) {
                __mongoRunner.RedirectConsoleOutput(_output.WriteLine)
                    .WithMongoBinDirectory(c => c.RelativeToSolutionDirectory(@"..\mongodb\bin"))
                    .WithConfigFile(c => c.RelativeToSolutionDirectory(@"..\mongodb\bin\mongod.cfg"))
                    .WithDataDirectory(c => c.RelativeToSolutionDirectory(@"..\mongodb\data"), createDirectoryIfNecessary: true)
                    .WithLogFile(c => c.RelativeToSolutionDirectory(@"..\mongodb\log\mongod.log"), createDirectoryIfNecessary: true)
                    .Start()
                    .EnsureReplicaSet();
            }

            __client = __mongoRunner.Connect();
        }
    }
}

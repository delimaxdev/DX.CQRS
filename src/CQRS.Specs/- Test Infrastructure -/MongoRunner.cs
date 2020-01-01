using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DX.Testing
{
    public class MongoRunner {
        private readonly List<string> _args = new List<string>();
        private readonly FilePath _mongoBinDirectory = new FilePath();
        private Action<string> _outputAction = Console.WriteLine;

        public string ConnectionString { get; } = "mongodb://localhost:27017";

        public MongoRunner RedirectConsoleOutput(Action<string> outputAction) {
            _outputAction = outputAction;
            return this;
        }

        public MongoRunner WithMongoBinDirectory(Action<FilePath> directoryConfig) {
            directoryConfig(_mongoBinDirectory);
            return this;
        }

        public MongoRunner WithDataDirectory(Action<FilePath> directoryConfig, bool createDirectoryIfNecessary = true) {
            FilePath p = FilePath.GetFilePath(directoryConfig);

            if (createDirectoryIfNecessary) {
                CreateDirectoryIfNecessary(p.GetPath());
            }

            AddArgWithPath("dbpath", p);
            return this;
        }

        public MongoRunner WithLogFile(Action<FilePath> filePathConfig, bool createDirectoryIfNecessary = true) {
            FilePath p = FilePath.GetFilePath(filePathConfig);

            if (createDirectoryIfNecessary) {
                string dir = Path.GetDirectoryName(p.GetPath());
                CreateDirectoryIfNecessary(dir);
            }

            AddArgWithPath("logpath", p);
            return this;
        }

        public MongoRunner WithConfigFile(Action<FilePath> filePathConfig) {
            AddArgWithPath("config", FilePath.GetFilePath(filePathConfig));
            return this;
        }

        public MongoRunner KillAllRunning() {
            foreach (var process in Process.GetProcessesByName("mongod")) {
                _outputAction("Killing mongod...");
                process.Kill();
                process.WaitForExit(30_000);
            }

            return this;
        }

        public MongoRunner Start() {
            if (Process.GetProcessesByName("mongod").Any()) {
                _outputAction("mongod is already running...");
                return this;
            }

            string mongoExe = FilePath.GetPath(c =>
                c.RelativeTo(_mongoBinDirectory, "mongod.exe"));

            string arguments = String.Join(" ", _args);

            Process process = new Process() {
                StartInfo = new ProcessStartInfo {
                    FileName = mongoExe,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Arguments = arguments
                }
            };

            process.OutputDataReceived += (sender, args) => _outputAction(args.Data ?? String.Empty);
            process.ErrorDataReceived += (sender, args) => _outputAction(args.Data ?? String.Empty);

            _outputAction($"Starting {mongoExe} {arguments}...");
            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();


            return this;
        }

        public MongoRunner EnsureReplicaSet() {
            var client = new MongoClient("mongodb://localhost:27017?connect=direct");
            var adminDB = client.GetDatabase("admin");

            try {
                adminDB.RunCommand<BsonDocument>("{ replSetGetStatus: 1 }");
            } catch (MongoCommandException ex) when (ex.CodeName == "NotYetInitialized") {
                _outputAction("Initializing replica set...");
                adminDB.RunCommand<BsonDocument>("{ replSetInitiate: 1 }");
            }

            return this;
        }

        public IMongoClient Connect() {
            return new MongoClient(ConnectionString);
        }

        public static bool ProcessIsAlreadyRunning() {
            return Process.GetProcessesByName("mongod").Any();
        }

        private void AddArgWithPath(string argName, FilePath path) {
            // If a path ends with a backslash, the startup of mongod fails
            // because the last backslash is interpreted as an escape for 
            // the double quotes (which are needed in case the path contains
            // white space).
            path.TrimTrailingSlash();

            string arg = $"--{argName} \"{path.GetPath()}\"";
            _args.Add(arg);
        }

        private void CreateDirectoryIfNecessary(string dir) {
            if (!Directory.Exists(dir)) {
                _outputAction($"Creating directory {dir}...");
                Directory.CreateDirectory(dir);
            }
        }
    }
}

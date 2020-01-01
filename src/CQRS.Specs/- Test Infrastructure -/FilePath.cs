using System;
using System.IO;
using System.Reflection;

namespace DX.Testing
{
    public class FilePath {
        private static FilePath __solutionDirectory = new FilePath();
        private static FilePath __baseDirectory = GetBaseDirectory();
        private string _path;

        public void RelativeToSolutionDirectory(string path) {
            RelativeTo(__solutionDirectory, path);
        }

        public void RelativeToBaseDirectory(string path) {
            RelativeTo(__baseDirectory, path);
        }

        public void RelativeTo(FilePath root, string path) {
            _path = Path.Combine(root._path, path);
        }

        public void Absolute(string path) {
            _path = path;
        }

        public void TrimTrailingSlash() {
            _path = _path.Trim('\\');
        }

        public string GetPath() {
            return Path.GetFullPath(_path);
        }

        public static void SetSolutionPath(Action<FilePath> configAction) {
            configAction(__solutionDirectory);
        }

        public static FilePath GetFilePath(Action<FilePath> configAction) {
            var p = new FilePath();
            configAction(p);
            return p;
        }

        public static string GetPath(Action<FilePath> configAction) {
            return GetFilePath(configAction).GetPath();
        }

        private static FilePath GetBaseDirectory() {
            FilePath baseDirectory = new FilePath();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string filePath = new Uri(assembly.CodeBase).LocalPath;
            baseDirectory.Absolute(Path.GetDirectoryName(filePath));

            return baseDirectory;
        }
    }
}

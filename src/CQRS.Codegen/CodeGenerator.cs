using Buildalyzer;
using Buildalyzer.Workspaces;
using DX.Cqrs.Codegen.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen {
    public partial class CodeGenerator {
        private readonly Project _project;

        public HashSet<string> DefaultUsings { get; } = new HashSet<string>();

        public static Task<CodeGenerator> CreateAsync(string projectFilePath) {
            AnalyzerManager manager = new AnalyzerManager();
            ProjectAnalyzer analyzer = manager.GetProject(projectFilePath);
            AdhocWorkspace workspace = new AdhocWorkspace();
            Project project = analyzer.AddToWorkspace(workspace);
            return Task.FromResult(new CodeGenerator(project));
        }

        public static CodeGenerator Create(string projectFilePath) {
            return CreateAsync(projectFilePath).Result;
        }

        private CodeGenerator(Project project) {
            _project = project;
        }

        public void GenerateAll(Action<string> writeAction) {
            writeAction("#nullable enable");

            Task[] tasks = _project
                .Documents
                .Where(d => d.Name.EndsWith(".contract.cs"))
                .Select(d => GenerateAndWrite(d, writeAction))
                .ToArray();

            Task.WaitAll(tasks);
        }

        private async Task GenerateAndWrite(Document document, Action<string> writeAction) {
            string code = await Generate(document);
            writeAction(code);
        }

        /// <summary>
        /// Just for Unit Testing.
        /// </summary>
        internal Task<string> Generate(string documentName) {
            Document doc = _project
                .Documents
                .Single(d => d.Name == documentName);

            return Generate(doc);
        }

        private async Task<string> Generate(Document document) {
            SyntaxNode sourceRoot = await document.GetSyntaxRootAsync();
            SemanticModel semanticModel = await document.GetSemanticModelAsync();

            SourceDocument sourceDoc = new SourceDocument(sourceRoot, semanticModel);

            List<MemberDeclarationSyntax> newMembers = new List<MemberDeclarationSyntax>();
            sourceDoc.GenerateCode(newMembers, DefaultUsings);

            CompilationUnitSyntax root = CompilationUnit()
                .WithMembers(List(newMembers))
                .NormalizeWhitespace();

            SyntaxTree tree = CSharpSyntaxTree.Create(root);
            return tree.ToString();
        }
    }

    [Serializable]
    public class CodeGenerationException : Exception {
        public CodeGenerationException() { }
        public CodeGenerationException(string message) : base(message) { }
        public CodeGenerationException(string message, Exception inner) : base(message, inner) { }
        protected CodeGenerationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}

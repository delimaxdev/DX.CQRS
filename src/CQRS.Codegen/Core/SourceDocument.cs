using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal class SourceDocument {
        private readonly SourceItem[] _sourceItems = { };
        private readonly List<UsingDirectiveSyntax>? _usings = new List<UsingDirectiveSyntax>();
        private readonly NamespaceDeclarationSyntax? _namespace;

        public SourceDocument(SyntaxNode sourceRoot, SemanticModel semanticModel) {
            NamespaceFinder finder = new NamespaceFinder();
            finder.Visit(sourceRoot);

            if (finder.Namespace != null) {
                _sourceItems = SourceItem.CreateNestedItems(finder.Namespace.Members, semanticModel);
                _usings = finder.Usings;
                _namespace = finder.Namespace;
            }
        }

        public void GenerateCode(List<MemberDeclarationSyntax> newMembers, HashSet<string> defaultUsings) {
            if (_namespace == null)
                return;

            var context = new GenerationContext(_sourceItems);
            foreach (SourceItem item in _sourceItems) {
                item.PrepareGeneration(context);
            }

            var nestedMembers = new List<MemberDeclarationSyntax>();
            foreach (SourceItem item in _sourceItems) {
                item.GenerateCode(nestedMembers, context);
            }

            newMembers.Add(
                NamespaceDeclaration(_namespace.Name)
                    .WithUsings(
                        List(defaultUsings
                            .Select(ns => UsingDirective(IdentifierName(ns)))
                            .Concat(_usings)))
                    .WithMembers(List(nestedMembers))
            );
        }

        private class NamespaceFinder : CSharpSyntaxWalker {
            public NamespaceDeclarationSyntax? Namespace { get; private set; }

            public List<UsingDirectiveSyntax> Usings { get; private set; } = new List<UsingDirectiveSyntax>();

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node) {
                if (Namespace != null) {
                    throw new CodeGenerationException("A single file must not contain multiple namespaces.");
                }

                Namespace = node;
                Usings.AddRange(Namespace.Usings);
            }

            public override void VisitUsingDirective(UsingDirectiveSyntax node) {
                Usings.Add(node);
            }
        }
    }
}

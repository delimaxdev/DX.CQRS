using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;

namespace DX.Cqrs.Codegen.Core {
    internal abstract class SourceItem {
        public abstract void GenerateCode(List<MemberDeclarationSyntax> newMembers, GenerationContext context);

        public virtual void PrepareGeneration(GenerationContext context) { }

        public static SourceItem[] CreateNestedItems(SyntaxList<MemberDeclarationSyntax> members, SemanticModel semanticModel) =>
            members.OfType<TypeDeclarationSyntax>()
                .Select(d => TryCreateItem(d, semanticModel))
                .Where(i => i != null)
                .ToArray()!;

        private static SourceItem? TryCreateItem(TypeDeclarationSyntax declaration, SemanticModel semanticModel) {
            if (SourceContainerType.IsContainerType(declaration))
                return new SourceContainerType(declaration, semanticModel);

            return declaration is ClassDeclarationSyntax classDeclaration ?
                new SourceClass(semanticModel, classDeclaration) :
                null;
        }
    }
}

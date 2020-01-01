using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace DX.Cqrs.Codegen.Core
{
    internal class SourceInterface : SourceType {
        public InterfaceDeclarationSyntax Declaration { get; }

        public override IEnumerable<SourceProperty> Properties => DeclaredProperties;

        public SourceInterface(SemanticModel semanticModel, InterfaceDeclarationSyntax declaration)
            : base(semanticModel, declaration) {
            Declaration = declaration;
        }

        public override void GenerateCode(List<MemberDeclarationSyntax> newMembers, GenerationContext context) {
        }
    }
}

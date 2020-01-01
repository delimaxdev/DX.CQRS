using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal class SourceContainerType : SourceItem {

        public TypeDeclarationSyntax Declaration { get; }

        public SourceItem[] Items { get; } = { };

        public SourceContainerType(TypeDeclarationSyntax declaration, SemanticModel semanticModel) {
            Declaration = declaration;
            Items = CreateNestedItems(declaration.Members, semanticModel);
        }

        public override void PrepareGeneration(GenerationContext context) {
            foreach (SourceItem item in Items) {
                item.PrepareGeneration(context);
            }
        }

        public override void GenerateCode(List<MemberDeclarationSyntax> newMembers, GenerationContext context) {
            List<MemberDeclarationSyntax> nestedMembers = new List<MemberDeclarationSyntax>();

            foreach (SourceItem item in Items) {
                item.GenerateCode(nestedMembers, context);
            }

            TypeDeclarationSyntax type = Declaration switch
            {
                ClassDeclarationSyntax @class => (TypeDeclarationSyntax)ClassDeclaration(Declaration.Identifier),
                InterfaceDeclarationSyntax @interface => InterfaceDeclaration(Declaration.Identifier),
                _ => throw new NotImplementedException()
            };

            newMembers.Add(type
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
                .WithMembers(List(nestedMembers)));
        }

        public static bool IsContainerType(TypeDeclarationSyntax declaration) {
            return declaration.Members.OfType<ClassDeclarationSyntax>().Any();
        }
    }
}
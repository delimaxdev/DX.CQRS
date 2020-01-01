using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal class SourceProperty {
        private readonly SemanticModel _semanticModel;

        public SimpleNameSyntax Name => IdentifierName(Declaration.Identifier);

        public TypeSyntax Type => Declaration.Type;

        public ITypeSymbol TypeSymbol => _semanticModel
            .GetDeclaredSymbol(Declaration)
            .Type;

        public bool IsNullable =>
            Type is NullableTypeSyntax || (
                TypeSymbol is INamedTypeSymbol named &&
                named.IsGenericType &&
                named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T);

        public PropertyDeclarationSyntax Declaration { get; }

        public SourceType DeclaringType { get; }

        public SourceProperty(SemanticModel semanticModel, PropertyDeclarationSyntax declaration, SourceType declaringType)
            => (_semanticModel, Declaration, DeclaringType) = (semanticModel, declaration, declaringType);


        internal PropertyDeclarationSyntax GenerateReadonlyDeclaration() =>
            GeneratePropertyDeclarationBase(Type);

        internal MemberAccessExpressionSyntax GetAccessExpression(SimpleNameSyntax sourceObjectVariableName) =>
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                sourceObjectVariableName,
                Name);
        
        internal StatementSyntax GetAssignmentStatement(ExpressionSyntax sourceExpression) =>
            ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    left: Name,
                    right: sourceExpression));

        internal PropertyDeclarationSyntax GeneratePropertyDeclarationBase(TypeSyntax type, string? name = null, AccessorDeclarationSyntax[]? accessors = null) =>
             PropertyDeclaration(type, name != null ? Identifier(name) : Declaration.Identifier)
                .WithoutLeadingTrivia() // https://github.com/dotnet/roslyn/issues/4673
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithAttributeLists(Declaration.AttributeLists)
                .AddAccessorListAccessors(accessors ?? AutoGetAccessor())
                .WithLeadingTrivia(Declaration.GetLeadingTrivia());

        private AccessorDeclarationSyntax[] AutoGetAccessor() =>
            new[] { AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)) };

    }
}
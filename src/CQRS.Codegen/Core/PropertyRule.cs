using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal enum AssignmentTarget {
        SourceClass,
        SourceToSource,
        BuilderClass
    }

    internal abstract class PropertyRule {
        private static readonly List<PropertyRule> Rules = new List<PropertyRule> {
            new KeyedPropertyRule(),
            new ArrayPropertyRule(),
            new NotNullablePropertyRule(),
            new DefaultPropertyRule()
        };

        public abstract bool CanHandle(SourceProperty p);

        public abstract TypeSyntax GetBuilderPropertyType(SourceProperty p);

        public virtual IEnumerable<PropertyDeclarationSyntax> GenerateBuilderPropertyDeclarations(SourceProperty p) =>
            new[] { GenerateBuilderPropertyDeclaration(p) };

        protected virtual PropertyDeclarationSyntax GenerateBuilderPropertyDeclaration(SourceProperty p) =>
            p.GeneratePropertyDeclarationBase(GetBuilderPropertyType(p))
                .AddAccessorListAccessors(
                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

        protected PropertyDeclarationSyntax GetReadonlyPropertyDeclaration(SourceProperty p, ExpressionSyntax initializer) =>
            p.GeneratePropertyDeclarationBase(GetBuilderPropertyType(p))
                .WithInitializer(EqualsValueClause(initializer))
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        public abstract StatementSyntax GetAssignmentStatement(
            AssignmentTarget target,
            SourceProperty p,
            IdentifierNameSyntax sourceObjectVariableName);

        protected static StatementSyntax GetSourceMethodCallAssignmentStatement(
            SourceProperty p,
            SimpleNameSyntax sourceObjectVariableName,
            string methodName
        ) => p.GetAssignmentStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        p.GetAccessExpression(sourceObjectVariableName),
                        IdentifierName(methodName))));

        protected static StatementSyntax GetTargetMethodCallAssignmentStatement(
            SourceProperty p,
            SimpleNameSyntax sourceObjectVariableName,
            string methodName
        ) => ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        p.Name,
                        IdentifierName(methodName)))
                .AddArgumentListArguments(Argument(p.GetAccessExpression(sourceObjectVariableName))));

        protected static StatementSyntax GetConstructorAssignmentStatement(
            SourceProperty p,
            TypeSyntax constructorType,
            SimpleNameSyntax sourceObjectVariableName
        ) => p.GetAssignmentStatement(
                ObjectCreationExpression(constructorType)
                    .AddArgumentListArguments(
                        Argument(p.GetAccessExpression(sourceObjectVariableName))));

        protected static ExpressionSyntax GetConstructorCallInitializer(TypeSyntax type) =>
            ObjectCreationExpression(type)
                .WithArgumentList(ArgumentList());

        internal static PropertyRule GetRule(SourceProperty p)
            => Rules.First(r => r.CanHandle(p));
    }

    internal abstract class CollectionPropertyRule : PropertyRule {
        protected SimpleNameSyntax GetArrayTypeName(SourceProperty p) {
            if (p.TypeSymbol is IArrayTypeSymbol arrayType) {
                if (arrayType.ElementType is INamedTypeSymbol n && n.TypeArguments.Any()) {
                    return GenericName(n.Name)
                        .AddTypeArgumentListArguments(
                            n.TypeArguments.Select(arg => IdentifierName(arg.Name)).ToArray());
                } else {
                    return IdentifierName(arrayType.ElementType.Name);

                }
            }

            throw new InvalidOperationException("Property is not of Array type.");
        }

        protected PropertyDeclarationSyntax CreateItemsProperty(SourceProperty p, TypeSyntax itemType) =>
            p.GeneratePropertyDeclarationBase(
                GenericName("IEnumerable").AddTypeArgumentListArguments(itemType),
                $"{p.Name}Items",
                new[] {
                    AccessorDeclaration(
                        SyntaxKind.SetAccessorDeclaration,
                        Block(
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        p.Name,
                                        IdentifierName("Clear")))),
                            ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        p.Name,
                                        IdentifierName("AddRange")))
                                .AddArgumentListArguments(Argument(IdentifierName("value")))))) })
            .WithAttributeLists(
                SingletonList(
                    AttributeList(
                        SingletonSeparatedList(
                            Attribute(IdentifierName("JsonIgnore"))))));
    }

    internal class KeyedPropertyRule : CollectionPropertyRule {
        public override bool CanHandle(SourceProperty p)
            => IsKeyedCollection(p) || IsKeyedLookup(p);

        public override TypeSyntax GetBuilderPropertyType(SourceProperty p)
            => ExtractPropertyType(ExtractInitializerExpression(p)) ?? p.Type;

        public override IEnumerable<PropertyDeclarationSyntax> GenerateBuilderPropertyDeclarations(SourceProperty p) {
            return IsKeyedCollection(p) ?
                base.GenerateBuilderPropertyDeclarations(p).Concat(new[] { CreateItemsProperty(p, GetItemTypeName(p)) }) :
                base.GenerateBuilderPropertyDeclarations(p);
        }

        protected override PropertyDeclarationSyntax GenerateBuilderPropertyDeclaration(SourceProperty p) =>
            GetReadonlyPropertyDeclaration(
                p,
                ExtractInitializerExpression(p) ?? LiteralExpression(SyntaxKind.NullLiteralExpression));

        public override StatementSyntax GetAssignmentStatement(AssignmentTarget target, SourceProperty p, IdentifierNameSyntax sourceObjectVariableName) {
            return target switch
            {
                AssignmentTarget.SourceClass =>
                    GetSourceMethodCallAssignmentStatement(p, sourceObjectVariableName, "ToImmutable"),
                AssignmentTarget.SourceToSource =>
                    p.GetAssignmentStatement(p.GetAccessExpression(sourceObjectVariableName)),
                _ => GetTargetMethodCallAssignmentStatement(p, sourceObjectVariableName, "AddRange")
            };
        }

        private TypeSyntax GetItemTypeName(SourceProperty p) {
            var genericName = (GenericNameSyntax)p.Type;
            return genericName.TypeArgumentList.Arguments.Last();
        }

        private ExpressionSyntax? ExtractInitializerExpression(SourceProperty p) {
            MethodDeclarationSyntax? initMethod = p.DeclaringType
                .Declaration
                .Members.OfType<MethodDeclarationSyntax>()
                .SingleOrDefault(c => c.Identifier.Text == "Initialize");

            IEnumerable<StatementSyntax> initializationStatements = initMethod != null ?
                initMethod.Body.Statements :
                Enumerable.Empty<StatementSyntax>();

            return initializationStatements
                .OfType<ExpressionStatementSyntax>()
                .Select(exp => exp.Expression as InvocationExpressionSyntax)
                .Where(exp => IsBuilderSetInvocation(exp?.Expression as MemberAccessExpressionSyntax))
                .Select(x => (
                    FirstArg: x.ArgumentList.Arguments[0].Expression,
                    SecondArg: x.ArgumentList.Arguments[1].Expression))
                .Where(x => x.FirstArg is SimpleNameSyntax n && n.Identifier.Text == p.Name.Identifier.Text)
                .Select(x => x.SecondArg)
                .SingleOrDefault();

            static bool IsBuilderSetInvocation(MemberAccessExpressionSyntax? m) =>
                m != null &&
                m.Expression is SimpleNameSyntax n &&
                n.Identifier.Text == "Builder" &&
                m.Name.Identifier.Text == "Set";
        }

        private TypeSyntax? ExtractPropertyType(ExpressionSyntax? assignmentExpression) {
            return assignmentExpression is ObjectCreationExpressionSyntax exp ?
                exp.Type :
                null;
        }

        private bool IsKeyedLookup(SourceProperty p)
            => GetCollectionType(p) == "IKeyedLookup";

        private bool IsKeyedCollection(SourceProperty p)
            => GetCollectionType(p) == "IKeyed";

        private string? GetCollectionType(SourceProperty p) =>
            p.Type is GenericNameSyntax genericName ?
                genericName.Identifier.Text :
                null;
    }

    internal class ArrayPropertyRule : CollectionPropertyRule {
        public override bool CanHandle(SourceProperty p)
            => p.TypeSymbol is IArrayTypeSymbol;

        public override TypeSyntax GetBuilderPropertyType(SourceProperty p)
            => GenericName("List").AddTypeArgumentListArguments(GetArrayTypeName(p));

        protected override PropertyDeclarationSyntax GenerateBuilderPropertyDeclaration(SourceProperty p) =>
            GetReadonlyPropertyDeclaration(
                p,
                GetConstructorCallInitializer(GetBuilderPropertyType(p)));

        public override IEnumerable<PropertyDeclarationSyntax> GenerateBuilderPropertyDeclarations(SourceProperty p)
            => base.GenerateBuilderPropertyDeclarations(p).Concat(new[] { CreateItemsProperty(p, GetArrayTypeName(p)) });

        public override StatementSyntax GetAssignmentStatement(
            AssignmentTarget target,
            SourceProperty p,
            IdentifierNameSyntax sourceObjectVariableName
        ) {
            return target switch
            {
                AssignmentTarget.SourceClass =>
                    GetSourceMethodCallAssignmentStatement(p, sourceObjectVariableName, "ToArray"),
                AssignmentTarget.SourceToSource =>
                    p.GetAssignmentStatement(p.GetAccessExpression(sourceObjectVariableName)),
                _ =>
                    GetTargetMethodCallAssignmentStatement(p, sourceObjectVariableName, "AddRange")
            };
        }
    }

    internal class NotNullablePropertyRule : PropertyRule {
        public override bool CanHandle(SourceProperty p)
            => !p.IsNullable;

        public override TypeSyntax GetBuilderPropertyType(SourceProperty p)
            => NullableType(p.Type);

        public override StatementSyntax GetAssignmentStatement(
            AssignmentTarget target,
            SourceProperty p,
            IdentifierNameSyntax sourceObjectVariableName
        ) {
            switch (target) {
                case AssignmentTarget.SourceClass:
                    return p.GetAssignmentStatement(AddNullCheck(p.GetAccessExpression(sourceObjectVariableName)));
                default:
                    return p.GetAssignmentStatement(p.GetAccessExpression(sourceObjectVariableName));
            }
        }

        private static ExpressionSyntax AddNullCheck(ExpressionSyntax exp) =>
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    exp,
                    IdentifierName("NotNull")));
    }

    internal class DefaultPropertyRule : PropertyRule {
        public override bool CanHandle(SourceProperty p)
            => true;

        public override TypeSyntax GetBuilderPropertyType(SourceProperty p)
            => p.Type;

        public override StatementSyntax GetAssignmentStatement(
            AssignmentTarget target,
            SourceProperty p,
            IdentifierNameSyntax sourceObjectVariableName
        ) {
            return p.GetAssignmentStatement(p.GetAccessExpression(sourceObjectVariableName));
        }
    }
}

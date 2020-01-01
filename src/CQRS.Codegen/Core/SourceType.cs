using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal abstract class SourceType : SourceItem {
        public SourceProperty[] DeclaredProperties { get; }

        public abstract IEnumerable<SourceProperty> Properties { get; }

        public string Name => Declaration.Identifier.Text;

        public IdentifierNameSyntax NameSyntax => IdentifierName(Declaration.Identifier);

        public TypeDeclarationSyntax Declaration { get; }

        public SourceType(SemanticModel semanticModel, TypeDeclarationSyntax declaration) {
            DeclaredProperties = declaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Select(d => new SourceProperty(semanticModel, d, this))
                .ToArray();

            Declaration = declaration;
        }

        public IEnumerable<StatementSyntax> GenerateConditionalPropertyAssginmentBlock(AssignmentTarget target, IdentifierNameSyntax sourceObjectVariableName) =>
            new StatementSyntax[] {
                IfStatement(
                    condition: BinaryExpression(
                        SyntaxKind.NotEqualsExpression,
                        left: sourceObjectVariableName,
                        right: LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    statement: Block(GeneratePropertyAssginmentBlock(target, sourceObjectVariableName)))
            };

        public IEnumerable<StatementSyntax> GeneratePropertyAssginmentBlockWithNullCheck(AssignmentTarget target, IdentifierNameSyntax sourceObjectVariableName) =>
            new[] { GenerateNullCheck(sourceObjectVariableName) }.Concat(GeneratePropertyAssginmentBlock(target, sourceObjectVariableName));

        private IEnumerable<StatementSyntax> GeneratePropertyAssginmentBlock(AssignmentTarget target, IdentifierNameSyntax sourceObjectVariableName) =>
            Properties.Select(p =>
                PropertyRule.GetRule(p).GetAssignmentStatement(target, p, sourceObjectVariableName));

        private StatementSyntax GenerateNullCheck(IdentifierNameSyntax variableName) =>
            IfStatement(
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    variableName,
                    LiteralExpression(
                        SyntaxKind.NullLiteralExpression)),
                ThrowStatement(
                    ObjectCreationExpression(IdentifierName("ArgumentNullException"))
                        .AddArgumentListArguments(
                            Argument(
                                InvocationExpression(IdentifierName("nameof")).AddArgumentListArguments(
                                    Argument(variableName))))));
    }
}

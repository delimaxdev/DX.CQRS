using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core
{
    internal class ClassGenerator {
        private static readonly SyntaxKind[] VisibilityKinds = { SyntaxKind.InternalKeyword, SyntaxKind.PublicKeyword };

        private ClassDeclarationSyntax _declaration;
        private readonly List<ConstructorGenerator> _constructors = new List<ConstructorGenerator>();

        /// <summary>
        ///   Creates a "partial class" declaration with the same name as the <paramref name="template"/>.
        /// </summary>
        public ClassGenerator(ClassDeclarationSyntax template) {
            _declaration = ClassDeclaration(template.Identifier);
            MakePartial();
        }

        /// <summary>
        ///   Creates a "partial class" declaration with the given <paramref name="name"/> and the visibility
        ///   and attributes of the given <paramref name="template"/>.
        /// </summary>
        public ClassGenerator(ClassDeclarationSyntax template, string name) {
            _declaration = ClassDeclaration(name);
            CopyVisibility(template);
            MakePartial();
        }

        public ConstructorGenerator AddConstructor() {
            var c = new ConstructorGenerator(_declaration);
            _constructors.Add(c);
            return c;
        }

        public void AddMembers(params MemberDeclarationSyntax[] members) =>
            _declaration = _declaration.AddMembers(members);

        public void AddBaseType(SourceType baseType) =>
            AddBaseType(baseType.NameSyntax);

        public void AddBaseType(TypeSyntax baseType) =>
            _declaration = _declaration.AddBaseListTypes(SimpleBaseType(baseType));

        public void ImplementInterface(SourceInterface @interface) =>
            _declaration = _declaration.AddMembers(
                @interface.DeclaredProperties
                    .Select(p => p.GenerateReadonlyDeclaration())
                    .ToArray());

        public void CopyAttributes(ClassDeclarationSyntax template) =>
            _declaration = _declaration.WithAttributeLists(template.AttributeLists);

        public void AddAttribute(AttributeSyntax attribute) =>
            _declaration = _declaration.AddAttributeLists(AttributeList().AddAttributes(attribute));

        public ClassDeclarationSyntax Generate()
            => _declaration.AddMembers(
                _constructors.Select(c => c.Generate()).ToArray());

        private void MakePartial() =>
            _declaration = _declaration.AddModifiers(Token(SyntaxKind.PartialKeyword));

        private void CopyVisibility(ClassDeclarationSyntax template) =>
            _declaration = _declaration.AddModifiers(template.Modifiers
                .Where(m => VisibilityKinds.Contains(m.Kind()))
                .ToArray());

        public class ConstructorGenerator {
            private readonly SyntaxToken _name;

            public List<ParameterSyntax> Parameters { get; } = new List<ParameterSyntax>();
            public List<ArgumentSyntax> BaseArguments { get; } = new List<ArgumentSyntax>();
            public List<StatementSyntax> Statements { get; } = new List<StatementSyntax>();

            private ConstructorDeclarationSyntax Declaration { get; set; }

            public ConstructorGenerator(ClassDeclarationSyntax parent) =>
                _name = parent.Identifier;

            public ParameterGenerator AddRequiredParameter(SourceType sourceType, string actualType, string name) =>
                new RequiredParameterGenerator(this, sourceType, IdentifierName(actualType), name);

            public ParameterGenerator AddOptionalParameter(SourceType sourceType, string name) =>
                new OptionalParameterGenerator(this, sourceType, sourceType.NameSyntax, name);


            public ConstructorGenerator AddStatements(IEnumerable<StatementSyntax> statements) {
                Statements.AddRange(statements);
                return this;
            }

            public ConstructorDeclarationSyntax Generate() =>
                ConstructorDeclaration(_name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddParameterListParameters(Parameters.ToArray())
                    .WithInitializer(
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(SeparatedList(BaseArguments))))
                    .WithBody(Block(Statements));
        }

        public abstract class ParameterGenerator {
            private readonly ConstructorGenerator _constructor;

            protected IdentifierNameSyntax Name { get; }

            protected SourceType SourceType { get; }

            protected TypeSyntax ActualType { get;  }

            public ParameterGenerator(ConstructorGenerator constructor, SourceType type, TypeSyntax actualType, string name) {
                Name = IdentifierName(name);
                SourceType = type;
                ActualType = actualType;

                _constructor = constructor;
                _constructor.Parameters.Add(CreateParameter());
            }

            public ParameterGenerator PassToBaseConstructor() {
                _constructor.BaseArguments.Add(Argument(Name));
                return this;
            }

            public ParameterGenerator AddPropertyAssignmentBlock(AssignmentTarget target) {
                _constructor.Statements.AddRange(GeneratePropertyAssignmentBlock(target));
                return this;
            }

            protected abstract IEnumerable<StatementSyntax> GeneratePropertyAssignmentBlock(AssignmentTarget target);

            protected virtual ParameterSyntax CreateParameter()
                => Parameter(Name.Identifier).WithType(ActualType);
        }

        private class RequiredParameterGenerator : ParameterGenerator {
            public RequiredParameterGenerator(ConstructorGenerator constructor, SourceType type, TypeSyntax actualType, string name) 
                : base(constructor, type, actualType, name) { }

            protected override IEnumerable<StatementSyntax> GeneratePropertyAssignmentBlock(AssignmentTarget target)
                => SourceType.GeneratePropertyAssginmentBlockWithNullCheck(target, Name);
        }

        private class OptionalParameterGenerator : ParameterGenerator {
            public OptionalParameterGenerator(ConstructorGenerator constructor, SourceType type, TypeSyntax actualType, string name)
                : base(constructor, type, NullableType(actualType), name) { }

            protected override IEnumerable<StatementSyntax> GeneratePropertyAssignmentBlock(AssignmentTarget target)
                => SourceType.GenerateConditionalPropertyAssginmentBlock(target, Name);

            protected override ParameterSyntax CreateParameter()
                => base.CreateParameter().WithDefault(
                    EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)));
        }
    }
}
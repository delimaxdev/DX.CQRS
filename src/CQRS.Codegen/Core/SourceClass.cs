using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DX.Cqrs.Codegen.Core {
    internal class SourceClass : SourceType {
        public new ClassDeclarationSyntax Declaration { get; }

        public SourceInterface[] Interfaces { get; }


        public override IEnumerable<SourceProperty> Properties
            => DeclaredProperties.Concat(AddedProperties_);

        public IEnumerable<SourceProperty> AddedProperties_
            => Interfaces.SelectMany(i => i.DeclaredProperties);


        public string BuilderClassName => $"{Name}Builder";

        private string BaseTypeName { get; }

        public SourceClass? BaseClass { get; private set; }

        public bool IsPartialClass => Declaration
            .Modifiers
            .Any(m => m.Kind() == SyntaxKind.PartialKeyword);

        public SourceClass(SemanticModel semanticModel, ClassDeclarationSyntax declaration)
            : base(semanticModel, declaration) {
            Declaration = declaration;

            INamedTypeSymbol model = semanticModel.GetDeclaredSymbol(declaration);

            Interfaces = model
                .Interfaces
                .Select(symbol => symbol.DeclaringSyntaxReferences.FirstOrDefault())
                .Where(r => r != null)
                .Select(r => new SourceInterface(semanticModel, (InterfaceDeclarationSyntax)r.GetSyntax()))
                .ToArray();

            BaseTypeName = model.BaseType.Name;
        }

        public override void PrepareGeneration(GenerationContext context) {
            if (BaseTypeName != "Object" && context.GetSourceClass(BaseTypeName) is Some<SourceClass> baseSourceClass) {
                BaseClass = baseSourceClass;
            }
        }

        public override void GenerateCode(List<MemberDeclarationSyntax> newMembers, GenerationContext context) {
            if (!IsPartialClass)
                return;

            newMembers.Add(GeneratePartialClassCompletion(context));
            newMembers.Add(GenerateBuilderClass(context));
        }

        private ClassDeclarationSyntax GeneratePartialClassCompletion(GenerationContext context) {
            ClassGenerator classGen = new ClassGenerator(Declaration);

            var p = classGen.AddConstructor()
                .AddRequiredParameter(this, BuilderClassName, "source")
                .AddPropertyAssignmentBlock(AssignmentTarget.SourceClass);

            if (BaseClass != null)
                p.PassToBaseConstructor();

            p = classGen.AddConstructor()
                .AddRequiredParameter(this, Name, "source")
                .AddPropertyAssignmentBlock(AssignmentTarget.SourceToSource);

            if (BaseClass != null)
                p.PassToBaseConstructor();

            classGen.AddAttribute(
                Attribute(IdentifierName("Builder")).AddArgumentListArguments(
                    AttributeArgument(
                        TypeOfExpression(IdentifierName(BuilderClassName)))));

            classGen.AddMembers(GenerateMutateMethod());

            foreach (SourceInterface i in Interfaces) {
                classGen.ImplementInterface(i);
            }

            GenerateICausesInterfaceImplementation(classGen, context);
            return classGen.Generate();
        }

        private ClassDeclarationSyntax GenerateBuilderClass(GenerationContext context) {
            ClassGenerator @class = new ClassGenerator(Declaration, BuilderClassName);

            @class.AddMembers(
                Properties.SelectMany(p => PropertyRule
                    .GetRule(p)
                    .GenerateBuilderPropertyDeclarations(p))
                .ToArray());

            if (BaseClass != null) {
                @class.AddBaseType(IdentifierName(BaseClass.BuilderClassName));
            }

            GenerateIBuildsInterfaceImplementation(@class);

            GenerateBuilderConstructor(@class);
            GenerateAdditionalConstructors(@class);
            
            return @class.Generate();
        }

        private void GenerateBuilderConstructor(ClassGenerator @class) {
            var ctor = @class
                .AddConstructor();

            var p = ctor
                .AddOptionalParameter(this, "source")
                .AddPropertyAssignmentBlock(AssignmentTarget.BuilderClass);

            if (BaseClass != null) {
                p.PassToBaseConstructor();
            }
        }

        private void GenerateAdditionalConstructors(ClassGenerator @class) {
            List<ParameterInfo> parameters = new List<ParameterInfo>();

            var ctor = @class
                .AddConstructor();

            if (BaseClass != null) {
                BaseClass.GetBuilderConstructorParameters(parameters);
                
                foreach (ParameterInfo p in parameters) {
                    ctor.AddOptionalParameter(p.Type, p.Name).PassToBaseConstructor();
                }
            }

            ParameterInfo[] additionalParams = GetAdditionalBuilderConstructorParameters().ToArray();
            
            foreach (ParameterInfo p in additionalParams) {
                ctor.AddOptionalParameter(p.Type, p.Name).AddPropertyAssignmentBlock(AssignmentTarget.BuilderClass);
            }

            if (parameters.Any() || additionalParams.Any()) {
                // Default constructor
                @class.AddConstructor();
            }
        }

        private void GetBuilderConstructorParameters(List<ParameterInfo> parameters) {
            if (BaseClass == null) {
                parameters.Add(new ParameterInfo("source", this));
                return;
            }

            BaseClass.GetBuilderConstructorParameters(parameters);
            parameters.AddRange(GetAdditionalBuilderConstructorParameters());
        }

        private IEnumerable<ParameterInfo> GetAdditionalBuilderConstructorParameters() {
            return Interfaces
                .Where(i => i.DeclaredProperties.Any())
                .Select(i => {
                    string parameterName = $"source{i.Name}";
                    return new ParameterInfo(parameterName, i);
                });
        }

        private void GenerateIBuildsInterfaceImplementation(ClassGenerator @class) {
            @class.AddBaseType(GenericName("IBuilds").AddTypeArgumentListArguments(NameSyntax));

            MethodDeclarationSyntax buildMethod = MethodDeclaration(returnType: NameSyntax, Identifier("Build"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddBodyStatements(
                    ReturnStatement(
                        ObjectCreationExpression(NameSyntax)
                        .AddArgumentListArguments(Argument(ThisExpression()))));

            ConversionOperatorDeclarationSyntax implicitCastOperator =
                ConversionOperatorDeclaration(Token(SyntaxKind.ImplicitKeyword), NameSyntax)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                    .AddParameterListParameters(
                            Parameter(Identifier("builder"))
                                .WithType(IdentifierName(BuilderClassName)))
                   .AddBodyStatements(
                        ReturnStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("builder"),
                                    IdentifierName("Build")))));

            if (BaseClass != null)
                buildMethod = buildMethod.AddModifiers(Token(SyntaxKind.NewKeyword));

            @class.AddMembers(buildMethod, implicitCastOperator);
        }

        private void GenerateICausesInterfaceImplementation(ClassGenerator @class, GenerationContext context) {
            if (ImplementsICauses(out string? eventClassName) &&
                context.GetSourceClass(eventClassName) is Some<SourceClass> eventClassValue) {
                SourceClass eventClass = eventClassValue;


                List<ParameterInfo> builderParams = new List<ParameterInfo>();
                eventClass.GetBuilderConstructorParameters(builderParams);
                int parameterCount = builderParams.Count;

                MethodDeclarationSyntax buildEventMethod = MethodDeclaration(returnType: eventClass.NameSyntax, Identifier("BuildEvent"))
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddBodyStatements(
                        ReturnStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    ObjectCreationExpression(IdentifierName(eventClass.BuilderClassName))
                                        .AddArgumentListArguments(
                                            Enumerable.Repeat(Argument(ThisExpression()), parameterCount).ToArray()),
                                    IdentifierName("Build")))));

                @class.AddMembers(buildEventMethod);
            }
        }

        private MethodDeclarationSyntax GenerateMutateMethod() {
            return MethodDeclaration(NameSyntax, Identifier("Mutate"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("mutation"))
                        .WithType(GenericName(Identifier("Action"))
                            .AddTypeArgumentListArguments(IdentifierName(BuilderClassName))))
                .WithBody(
                    Block(
                        LocalDeclarationStatement(
                            VariableDeclaration(IdentifierName(BuilderClassName))
                                .AddVariables(
                                    VariableDeclarator(Identifier("mutator"))
                                        .WithInitializer(
                                            EqualsValueClause(
                                                ObjectCreationExpression(IdentifierName(BuilderClassName))
                                                    .AddArgumentListArguments(Argument(ThisExpression())))))),
                        ExpressionStatement(
                            InvocationExpression(IdentifierName("mutation"))
                                .AddArgumentListArguments(Argument(IdentifierName("mutator")))),
                        ReturnStatement(IdentifierName("mutator"))));
        }

        private bool ImplementsICauses(out string? genericArgument) {
            if (Declaration.BaseList != null) {
                GenericNameSyntax? @interface = Declaration.BaseList.Types
                    .Select(b => b.Type)
                    .OfType<GenericNameSyntax>()
                    .FirstOrDefault(n => n.Identifier.Text == "ICauses");

                if (@interface != null) {
                    genericArgument = ((SimpleNameSyntax)@interface.TypeArgumentList.Arguments[0]).Identifier.Text;
                    return true;
                }
            }

            genericArgument = default;
            return false;
        }

        private class ParameterInfo {
            public SourceType Type { get; }

            public string Name { get; }

            public ParameterInfo(string name, SourceType type) =>
                (Name, Type) = (name, type);
        }
    }
}

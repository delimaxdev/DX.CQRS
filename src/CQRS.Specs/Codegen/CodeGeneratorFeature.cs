using DX.Cqrs.Codegen;
using DX.Testing;
using FluentAssertions;
using Xunit.Abstractions;

namespace Codegen
{
    public class CodeGeneratorFeature : Feature {
        private const string SampleProjectPath = @"..\..\..\..\Certification\Certification.csproj";
        private readonly ITestOutputHelper _output;

        public CodeGeneratorFeature(ITestOutputHelper output) {
            _output = output;
        }
        
        //[Scenario]
        internal void VariousClasses(CodeGenerator gen, string code) {
            Given["a code generator"] = async () => gen = await CodeGenerator.CreateAsync(SampleProjectPath);
            When["generating code for a file"] = async () => {
                code = await gen.Generate("Client.contract.cs");
                //code = await gen.Generate("SampleClass.contract.cs");
                _output.WriteLine(code);
            };

            THEN["the correct namespace is declared"] = () => code.Should().Contain("namespace Codegen");
            AND["partial completions declare an appropriate constructor"] = () => code.Should().Contain("public SampleDO(SampleDOBuilder source)");
            AND["that ctor initializes the class properties from the source object"] = () => {
                code.Should().Contain("StringProp = source.StringProp.NotNull();");
                //code.Should().Contain("NullableStringProp = source.NullableStringProp;");
                code.Should().Contain("DateTimeProp = source.DateTimeProp.NotNull();");
                code.Should().Contain("NullableDateTimeProp = source.NullableDateTimeProp;");
                code.Should().Contain("VerboseNullableProp = source.VerboseNullableProp;");
            };

            AND["interfaces are implemented"] = () => code.Should().Contain("public string InterfaceStringProp");
            AND["builders derive from each other"] = () => code.Should().Contain("SampleEventBuilder : SampleDOBuilder");
            AND["declare appropriate constructors"] = () => {
                code.Should().Contain("public SampleDOBuilder(SampleDO? source)");
                code.Should().Contain("public SampleEventBuilder(SampleEvent? source)");
            };
        }
    }
}

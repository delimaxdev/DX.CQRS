using DX.Cqrs.Codegen;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

namespace CQRS.Codegen {
    class Program {


        [Argument(0, Description = "Pfad zur Projekt Datei", ShowInHelpText = true)]
        [Required]
        public string ProjectFilePath { get; }


        [Argument(1, Description = "Output to file", ShowInHelpText = true)]
        [Required]
        public string OutputFile { get; }

        static Task Main(string[] args) {

            return CommandLineApplication.ExecuteAsync<Program>(args);
        }

        internal async Task OnExecute()
        {
            CodeGenerator gen = await CodeGenerator.CreateAsync(ProjectFilePath);
            gen.DefaultUsings.Add("System");
            gen.DefaultUsings.Add("System.Collections.Generic");
            gen.DefaultUsings.Add("DX");
            gen.DefaultUsings.Add("DX.Contracts");
            gen.DefaultUsings.Add("Newtonsoft.Json");


            using FileStream fileStream = File.Open(OutputFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter writer = new StreamWriter(fileStream);
            gen.GenerateAll(writer.WriteLine);
            Console.WriteLine("Finished...");
        }
    }
}

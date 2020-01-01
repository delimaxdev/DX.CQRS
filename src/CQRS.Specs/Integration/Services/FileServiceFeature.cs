using DX.Cqrs.Services;
using DX.Testing;
using FluentAssertions;
using System.IO;
using System.Threading.Tasks;
using Xbehave;
using Xunit.Abstractions;

namespace Integratio.Services
{
    public class FileServiceIntegration : MongoFeature {
        public FileServiceIntegration(ITestOutputHelper output) 
            : base(output) { }

        [Scenario]
        internal void SaveGetDelete(MongoFileService fs, IFileID id, string content, string actual) {
            GIVEN["a service instance"] = () => fs = new MongoFileService(Env.DB);

            When["saving a file"] = async () => id = await fs.Save(async stream => {
                using (var w = new StreamWriter(stream)) {
                    await w.WriteAsync(content = "file contents");
                    await w.FlushAsync();
                }
            });

            And["getting the file"] = () => fs.Get(id, async stream =>
                actual = await new StreamReader(stream).ReadToEndAsync()
            );

            THEN["the returned file equals the original"] = () => actual.Should().Be(content);

            When["deleting the file"] = () => fs.Delete(id);

            Then["getting the file", ThrowsA<FileNotFoundException>()] = () => fs.Get(id, s => Task.CompletedTask);
            And["deleting the file", ThrowsA<FileNotFoundException>()] = () => fs.Delete(id);
        }
    }
}

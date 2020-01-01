using System;
using System.IO;
using System.Threading.Tasks;

namespace DX.Cqrs.Services
{
    public interface IFileService {
        Task<IFileID> Save(Func<Stream, Task> saveAction);

        Task Get(IFileID id, Func<Stream, Task> getAction);

        Task Delete(IFileID id);
    }

    public interface IFileID { }
}

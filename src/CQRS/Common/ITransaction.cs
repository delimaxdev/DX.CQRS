using System;
using System.Threading.Tasks;

namespace DX.Cqrs.Common {
    public interface ITransaction : IDisposable {
        Task CommitAsync();

        Task AbortAsync();
    }
}
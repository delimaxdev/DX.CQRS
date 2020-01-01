using DX.Messaging;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ITaskQueue;

namespace DX.Cqrs.Commands {
    public interface ITaskQueue : IReceives<Enqueue> {
        public class Enqueue : IMessage<Task> {
            public ITaskQueueItem Item { get; }

            public Enqueue(ITaskQueueItem item)
                => Item = Check.NotNull(item, nameof(item));
        }
    }

    public interface ITaskQueueItem {
        object TargetID { get; }

        Task Completion { get; }

        Task Process();
    }
}

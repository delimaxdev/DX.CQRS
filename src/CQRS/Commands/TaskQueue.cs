using DX.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using static DX.Cqrs.Commands.ITaskQueue;

namespace DX.Cqrs.Commands {
    internal abstract class TaskQueueItem : ITaskQueueItem {
        private static readonly object __voidObject = new Object();
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public abstract object TargetID { get; }

        public Task Completion => _tcs.Task;

        async Task ITaskQueueItem.Process() {
            try {
                await ProcessCore();
                _tcs.SetResult(__voidObject);
            } catch (Exception ex) {
                _tcs.SetException(ex);
            }
        }

        protected abstract Task ProcessCore();
    }

    internal abstract class TaskQueueItem<TResult> : ITaskQueueItem {
        private readonly TaskCompletionSource<TResult> _tcs = new TaskCompletionSource<TResult>();

        public abstract object TargetID { get; }

        public Task<TResult> Completion => _tcs.Task;

        Task ITaskQueueItem.Completion => _tcs.Task;

        async Task ITaskQueueItem.Process() {
            try {
                TResult result = await ProcessCore();
                _tcs.SetResult(result);
            } catch (Exception ex) {
                _tcs.SetException(ex);
            }
        }

        protected abstract Task<TResult> ProcessCore();
    }

    public partial class TaskQueue : ITaskQueue {
        public async Task Send(Enqueue message) {
            bool success = await _dispatcherBlock.SendAsync(message.Item);

            if (!success) {
                // TODO: Better handling!
                throw new InvalidOperationException();
            }
        }
    }

    partial class TaskQueue : Receivable {
        private readonly Worker[] _workers;
        private readonly ActionBlock<ITaskQueueItem> _dispatcherBlock;

        public TaskQueue(int degreeOfParallelism, int capacity = 1000) {
            Check.Requires(degreeOfParallelism > 0, nameof(degreeOfParallelism));

            _workers = Enumerable
                .Range(1, degreeOfParallelism)
                .Select(i => new Worker(capacity))
                .ToArray();


            if (capacity != -1) {
                // We substract the degreeOfParallelism here, because at least one item is being processed
                // in each worker (multiple items if they have the same TargetID).
                capacity = Math.Max(capacity - degreeOfParallelism, 1);
            }

            _dispatcherBlock = new ActionBlock<ITaskQueueItem>(
                Dispatch,
                new ExecutionDataflowBlockOptions { BoundedCapacity = capacity, MaxDegreeOfParallelism = 1 });

            Register<Enqueue, Task>(Send);
        }

        public async Task Shutdown() {
            _dispatcherBlock.Complete();
            await _dispatcherBlock.Completion;

            _workers.ForEach(w => w.Block.Complete());

            IEnumerable<Task> workerCompletionTasks = _workers
                .Select(w => w.Block.Completion);

            await Task.WhenAll(workerCompletionTasks);
        }


        private async Task Dispatch(ITaskQueueItem t) {
            Worker? worker = GetSpecificWorker(t.TargetID);

            if (worker == null) {
                worker = await WaitForIdleWorker();
                worker.Reassign(t.TargetID);
            }

            bool success = await worker.SendAsync(t);
            Expect.That(success); // TODO: can this be false?


            Worker? GetSpecificWorker(object targetID) {
                return _workers.SingleOrDefault(w => w.ProcessesTarget(targetID));
            };

            async Task<Worker> WaitForIdleWorker() {
                Worker w = _workers.FirstOrDefault(w => w.Last.IsCompleted);

                if (w == null) {
                    Task completedLast = await Task.WhenAny(_workers.Select(w => w.Last));
                    w = _workers.Single(w => w.Last == completedLast);
                }

                return w;
            };
        }

        private class Worker {
            private object? _targetID = null;

            public ActionBlock<ITaskQueueItem> Block { get; }

            public Task Last { get; private set; } = Task.CompletedTask;

            public Worker(int capacity) {
                var opt = new ExecutionDataflowBlockOptions {
                    BoundedCapacity = capacity,
                    MaxDegreeOfParallelism = 1
                };

                Block = new ActionBlock<ITaskQueueItem>(Process, opt);
            }

            public bool ProcessesTarget(object targetID) {
                return Object.Equals(_targetID, targetID);
            }

            public Task<bool> SendAsync(ITaskQueueItem t) {
                Last = t.Completion;
                return Block.SendAsync(t);
            }

            public void Reassign(object targetID) {
                _targetID = targetID;
                Last = Task.CompletedTask;
            }

            private Task Process(ITaskQueueItem t) {
                return t.Process();
            }
        }
    }
}

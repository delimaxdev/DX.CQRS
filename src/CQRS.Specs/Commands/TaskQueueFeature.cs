using DX.Cqrs.Commands;
using DX.Testing;
using FluentAssertions;
using FluentAssertions.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xbehave;
using static DX.Cqrs.Commands.ITaskQueue;

namespace Commands {
    public class TaskQueueFeature : Feature {
        [Scenario]
        internal void Parallelism(TaskQueue q, ParallelismDetector p1, ParallelismDetector p2) {
            GIVEN["a TaskQueue"] = () => q = CreateQueue(degreeOfParallelism: 4);

            When["enqueing 10 tasks for the same target and 10 tasks for different targets"] = () => {
                p1 = new ParallelismDetector();
                p2 = new ParallelismDetector();
                p1.Enqueue(q, targetIDs: Enumerable.Repeat(1, count: 10).Cast<object>());
                p2.Enqueue(q, targetIDs: Enumerable.Range(1, 10).Cast<object>());

                return q.Shutdown();
            };
            THEN["all tasks for the same target are run serially"] = () => p1.ActualParallism.Should().Be(1);
            AND["the tasks for differnet targets are run in parallel"] = () => p2.ActualParallism.Should().BeGreaterThan(1);
        }

        [Scenario]
        internal void TaskResults(TaskQueue q, Task<int>[] tasks) {
            GIVEN["a TaskQueue"] = () => q = CreateQueue(degreeOfParallelism: 2);

            When["enquing 10 tasks"] = () => {
                tasks = Enumerable.Range(1, 10)
                    .Select(i => {
                        var item = new DelegateItem<int>(createResultTask(i), i);
                        q.Send(new Enqueue(item)).Wait();
                        return item.Completion;
                    })
                    .ToArray();

                return q.Shutdown();
            };

            THEN["their results can be awaited"] = () =>
                tasks.Select(t => t.Result).Should().BeEquivalentTo(Enumerable.Range(1, 10));

            Func<Task<int>> createResultTask(int result)
                => () => Task.FromResult(result);
        }

        [Scenario]
        internal void ExceptionHandling(TaskQueue q, Task t, Exception ex) {
            GIVEN["a TaskQueue"] = () =>
                q = CreateQueue(degreeOfParallelism: 1);

            WHEN["a task throws an exception"] = () => {
                t = Enqueue(q, () => throw new InvalidOperationException(), 1);
            };

            Then["the exception is rethrown when awaiting its completion"] = async () => {
                try {
                    await t;
                } catch (Exception e) {
                    ex = e;
                }

                ex.Should().BeOfType<InvalidOperationException>();
            };

            WHEN["enqueing another task"] = () => t = Enqueue(q, () => Task.CompletedTask, 2);
            THEN["it is processed"] = () => {
                bool timeout = Task.WaitAny(new[] { t }, timeout: TimeSpan.FromMilliseconds(100)) == -1;
                timeout.Should().BeFalse();
            };
        }

        [Scenario]
        internal void TooManyTasks(TaskQueue q, Action action) {
            GIVEN["a TaskQueue with capacity 3"] = () => q = CreateQueue(degreeOfParallelism: 2, capacity: 3);

            AND["2 queued tasks"] = () => {
                Enqueue(q, () => Task.Delay(1000), 1);
                Enqueue(q, () => Task.Delay(1000), 2);
            };

            WHEN["enqueing another task"] = () =>
                action = () => Enqueue(q, () => Task.CompletedTask, 3);

            THEN["it should not block"] = () =>
                action.ExecutionTime().Should().BeLessThan(50.Milliseconds());

            WHEN["enqueing one more task"] = () =>
                action = () => Enqueue(q, () => Task.CompletedTask, 4);

            THEN["it should block until the first task is processed"] = () =>
                action.ExecutionTime().Should().BeGreaterThan(500.Milliseconds());
        }


        [Scenario]
        internal void Shutdown(TaskQueue q, Task[] tasks) {
            GIVEN["a TaskQueue"] = () => q = CreateQueue(degreeOfParallelism: 3);

            WHEN["enqueing 15 tasks"] = () => {
                tasks = Enumerable.Range(1, 15)
                    .Select(i => Enqueue(q, () => Task.Delay(50), new Object()))
                    .ToArray();
            };
            And["calling Shutdown"] = () => q.Shutdown();
            THEN["all tasks are completed"] = () => tasks.Should().OnlyContain(x => x.IsCompleted);
        }

        private TaskQueue CreateQueue(int degreeOfParallelism, int capacity = -1) {
            return new TaskQueue(degreeOfParallelism, capacity);
        }

        private static Task Enqueue(TaskQueue queue, Func<Task> task, object targetID = null) {
            DelegateItem item = new DelegateItem(task, targetID ?? new Object());
            queue.Send(new Enqueue(item)).Wait();
            return item.Completion;
        }

        private class DelegateItem : TaskQueueItem {
            private readonly Func<Task> _taskProvider;
            private readonly object _targetID;

            public override object TargetID => _targetID;

            public DelegateItem(Func<Task> taskProvider, object targetID)
                => (_taskProvider, _targetID) = (taskProvider, targetID);

            protected override Task ProcessCore()
                => _taskProvider();
        }

        private class DelegateItem<TResult> : TaskQueueItem<TResult> {
            private readonly Func<Task<TResult>> _taskProvider;
            private readonly object _targetID;

            public override object TargetID => _targetID;

            public DelegateItem(Func<Task<TResult>> taskProvider, object targetID)
                => (_taskProvider, _targetID) = (taskProvider, targetID);

            protected override Task<TResult> ProcessCore()
                => _taskProvider();
        }

        internal class ParallelismDetector {
            private readonly List<Task<int>> _tasks = new List<Task<int>>();

            public int ActualParallism => _tasks.Max(t => t.Result);

            public ParallelismDetector() {
            }

            public void Enqueue(TaskQueue q, IEnumerable<object> targetIDs) {
                _tasks.Clear();

                int i = 1;
                foreach (object targetID in targetIDs) {
                    Task<int> task = CreateTask(delay: i * 10);
                    _tasks.Add(task);

                    var item = new DelegateItem<int>(() => {
                        task.Start();
                        return task;
                    }, targetID);

                    q.Send(new Enqueue(item)).Wait();
                }
            }

            private Task<int> CreateTask(int delay) {
                return new Task<int>(() => {
                    Thread.Sleep(delay);
                    return _tasks.Count(x => x.Status == TaskStatus.Running);
                });
            }
        }
    }
}

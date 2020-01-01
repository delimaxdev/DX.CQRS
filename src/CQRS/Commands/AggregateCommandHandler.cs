using DX.Contracts;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;

namespace DX.Cqrs.Commands {
    public class AggregateCommandHandler<TAggregate> : Receivable, ICommandHandler where TAggregate : class, IPersistable, IReceivable {
        private readonly Dictionary<Type, StaticHandlerMethodInvoker?> _registeredMessages;

        public AggregateCommandHandler() {
            Register<HandleCommand, Task>(Receive);

            _registeredMessages = new[] { typeof(TAggregate) }
                .Concat(typeof(TAggregate).GetInterfaces())
                .SelectMany(t => t.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public))
                .Where(t => typeof(ICommandMessage).IsAssignableFrom(t))
                .ToDictionary(t => t, t => (StaticHandlerMethodInvoker?)null);

            RegisterStaticHandlerMethods();
        }

        private Maybe<Task> Receive(HandleCommand m) {
            Command command = m.Command;

            if (m.Command.Target != null) {
                if (_registeredMessages.TryGetValue(command.Message.GetType(), out StaticHandlerMethodInvoker? staticHandler)) {
                    return Dispatch(command.Message, command.Target!, staticHandler, m.Context);
                }
            }
            return None<Task>.Value;
        }

        private async Task Dispatch(ICommandMessage message, ID target, StaticHandlerMethodInvoker? staticHandler, IContext context) {
            var repository = context
                .Get<IServiceProvider>()
                .GetRequiredService<IRepository<TAggregate>>();

            TAggregate aggregate;

            if (staticHandler != null) {
                aggregate = staticHandler.Handle(target, message);
            } else {
                aggregate = await repository.Get(target);
                aggregate.Receive(message);
            }

            await repository.Save(aggregate);
        }

        private void RegisterStaticHandlerMethods() {
            var staticHandlerMethods = typeof(TAggregate)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => typeof(TAggregate).Equals(m.ReturnType))
                .Select(m => (Method: m, MessageType: GetMessageParameterType(m)))
                .Where(x => x.MessageType != null);

            foreach (var m in staticHandlerMethods) {
                // We override the normal registrations
                _registeredMessages[m.MessageType] = new StaticHandlerMethodInvoker(m.Method);
            }
        }

        private static Type? GetMessageParameterType(MethodInfo m) {
            if (m.ReturnType != typeof(TAggregate))
                return null;

            ParameterInfo[] ps = m.GetParameters();
            if (ps.Length != 2)
                return null;

            return ps[0].ParameterType;
        }

        private class StaticHandlerMethodInvoker {
            private readonly MethodInfo _method;

            public StaticHandlerMethodInvoker(MethodInfo method)
                => _method = method;

            public TAggregate Handle(ID target, ICommandMessage message)
                => (TAggregate)_method.Invoke(null, new object[] { message, target });
        }
    }
}
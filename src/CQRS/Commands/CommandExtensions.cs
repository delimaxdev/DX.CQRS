using DX.Contracts;
using DX.Cqrs.Common;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Cqrs.Commands {
    public static class CommandExtensions {

        public static Task Execute(this IServiceProvider services, ICommandMessage command, ID? targetID = null)
            => services.Execute(new Command(command, targetID));

        public static Task Execute(this IServiceProvider services, IBuilds<ICommandMessage> command, ID? targetID = null)
            => services.Execute(new Command(command.Build(), targetID));

        public static Task Execute<TRef>(this IServiceProvider services, ICommandMessage command, Ref<TRef> target) where TRef : IHasID<ID>
            => services.Execute(new Command(command, ID.FromRef(Check.NotNull(target, nameof(target)))));

        public static Task Execute<TRef>(this IServiceProvider services, IBuilds<ICommandMessage> message, Ref<TRef> target) where TRef : IHasID<ID>
            => services.Execute(new Command(message.Build(), ID.FromRef(Check.NotNull(target, nameof(target)))));

        public static Task Execute(this IServiceProvider services, Command command)
            => Execute(services, services.GetRequiredService<IContext>(), command);

        public static Task Execute(this IContext context, ICommandMessage command, ID targetID)
            => Execute(context.Get<IServiceProvider>(), context, new Command(command, targetID));

        private static async Task Execute(IServiceProvider services, IContext context, Command command) {
            QueueCommandResult result = await services
                .Send<QueueCommand, Task<QueueCommandResult>>(new QueueCommand(command, context))
                .To<ICommandProcessor>();

            await result.Completion;
        }
    }
}
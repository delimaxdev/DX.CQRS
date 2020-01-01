using Autofac;
using Autofac.Extensions.DependencyInjection;
using DX.Contracts;
using DX.Contracts.Serialization;
using DX.Cqrs.Application;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Cqrs.EventStore;
using DX.Messaging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using static DX.Cqrs.Commands.ICommandHandler;
using static DX.Cqrs.Commands.ICommandProcessor;

namespace DX.Testing {
    public class ContainerFeature : Feature {

        protected TestServices Setup(Action<TestServicesBuilder> config = null) {
            return new TestServices(b => {
                b.UseTransactionFake()
                    .UseEventStoreFake()
                    .UseRepositories()
                    .UseCommands()
                    .UseSerialization()
                    .UseContexts();

                config?.Invoke(b);
            });
        }
    }
}
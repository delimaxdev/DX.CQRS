using DX.Contracts;
using DX.Contracts.Cqrs.Domain;
using DX.Cqrs.Commands;
using DX.Cqrs.Common;
using DX.Cqrs.Commons;
using DX.Cqrs.Domain;
using DX.Testing;
using FluentAssertions;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xbehave;
using static DX.Cqrs.Commands.ICommandHandler;

namespace Commands
{
    public class AggregateCommandHandlerFeature : Feature {
        [Scenario]
        internal void Receive(
            AggregateCommandHandler<Customer> handler,
            RepositoryFake<Customer> repo,
            IContext context,
            Customer customer,
            ID customerID
        ) {
            GIVEN["a handler instance and a service provider"] = () => {
                handler = new AggregateCommandHandler<Customer>();
                IServiceProvider sp = new ServiceProviderFake().Register<IRepository<Customer>>(
                    repo = Substitute.ForPartsOf<RepositoryFake<Customer>>());
                context = new GenericContext();
                context.Set(sp, false);
            };

            When["sending a create message to the aggregate"] = () => Send(customerID = ID.NewID(), new Customer.Create());
            Then["the aggregate gets created"] = async () => customer = await repo.Get(customerID);
            AND["it receives the command"] = () => customer.Commands.Should().BeEmpty();

            When["sending a non-create message to the aggregate"] = () => {
                repo.ClearReceivedCalls();
                customer.Commands.Clear();
                return Send(customerID, new Customer.Promote());
            };
            Then["the aggregate is loaded and saved"] = async () => {
                await repo.Received().Get(customerID);
                await repo.Received().Save(customer);
            };
            AND["it receives the command"] = () => customer.Commands.Should().ContainSingle(m => m is Customer.Promote);

            When["sending a message that does not belong to the aggregate"] = () => {
                repo.ClearReceivedCalls();
                return Send(customerID, new Order.Ship());
            };
            THEN["no action is performed"] = () => repo.ReceivedCalls().Should().BeEmpty();


            Task Send(ID target, ICommandMessage message) {
                Maybe<Task> result = handler.Receive(new HandleCommand(
                    new Command(message, target), context));
                return result is Some<Task> t ? (Task)t : Task.CompletedTask;
            }
        }

        internal class Customer : AggregateRoot {
            public List<ICommandMessage> Commands { get; } = new List<ICommandMessage>();

            public Customer() {
                M.Handle<Create>(c => Commands.Add(c));
                M.Handle<Promote>(c => Commands.Add(c));
            }

            public static Customer Handle(Create command, ID customerID) {
                return new Customer() { ID = customerID };
            }

            public class Create : ICommandMessage { }

            public class Promote : ICommandMessage { }
        }

        internal class Order : AggregateRoot {
            public class Ship : ICommandMessage { }
        }
    }
}
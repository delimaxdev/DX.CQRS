using DX.Contracts;
using DX.Cqrs.Domain;
using DX.Cqrs.Domain.Core.Messaging;
using DX.Messaging;
using DX.Testing;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xbehave;

namespace Domain {
    public class MessengerFeature : Feature {
        [Scenario]
        [Example(typeof(TestEvent), new[] { "SUB-TE" })]
        [Example(typeof(BaseEvent), new[] { "ROOT-BE" })]
        [Example(typeof(DerivedEvent), new[] { "ROOT-DE", "SUB-DE", "ROOT-BE" })]
        internal void EventDispatching(
           Type eventType,
           string[] expectedInvocations,
           MessageDispatcher dispatcher,
           Messenger m,
           Messenger sub,
           List<string> invocations
       ) {
            GIVEN["a Messenger with some registrations a subordinate Messenger"] = () => {
                invocations = new List<string>();
                dispatcher = new MessageDispatcher(m = new Messenger());
                m.Apply<DerivedEvent>(e => invocations.Add("ROOT-DE"));
                m.Register(sub = new Messenger());
                m.Apply<BaseEvent>(e => invocations.Add("ROOT-BE"));
                sub.Apply<DerivedEvent>(e => invocations.Add("SUB-DE"));
                sub.Apply<TestEvent>(e => invocations.Add("SUB-TE"));
            };

            WHEN["applying the event"] = () => m.ApplyChange((IEvent)Activator.CreateInstance(eventType));
            THEN["the expected handlers are invoked"] = () => invocations.Should().BeEquivalentTo(expectedInvocations);

            WHEN["a subordinate Messenger applies the event"] = () => {
                invocations.Clear();
                sub.ApplyChange((IEvent)Activator.CreateInstance(eventType));
            };
            THEN["the same handlers are invoked"] = () => invocations.Should().BeEquivalentTo(expectedInvocations);
        }

        [Scenario]
        internal void MessageDispatching(
            MessageDispatcher dispatcher,
            Messenger m,
            Messenger sub,
            object result
        ) {
            GIVEN["a Messenger with some registrations a subordinate Messenger"] = () => {
                dispatcher = new MessageDispatcher(m = new Messenger());
                m.Handle<StringMessage1, string>(x => "ROOT-M1");
                m.Register(sub = new Messenger());
                m.Handle<IMessage<string>, string>(x => "ROOT-M2");
                sub.Handle<IMessage<string>, string>(x => "SUB-M2");
                sub.Handle<IMessage<int>, int>(x => 1234);
            };

            WHEN["sending a message"] = () => result = m.Send<StringMessage1, string>(new StringMessage1());
            THEN["the message is handled by the first concrete type registration"] = () => result.Should().Be("ROOT-M1");

            WHEN["sending a message"] = () => result = m.Send<StringMessage2, string>(new StringMessage2());
            THEN["the message is handled by a subtype registration"] = () => result.Should().Be("SUB-M2");

            WHEN["sending a message"] = () => result = m.Send<IntMessage, int>(new IntMessage());
            THEN["the message is forwarded to subordinate Messengers"] = () => result.Should().Be(1234);

            WHEN["a subordinate Messenger sends a message"] = () => result = sub.Send<StringMessage1, string>(new StringMessage1());
            THEN["it is handled by the root messenger"] = () => result.Should().Be("ROOT-M1");

            WHEN["a subordinate Messenger sends a message"] = () => result = sub.Send<IntMessage, int>(new IntMessage());
            THEN["it is forwarded to the subordinate Messenger"] = () => result.Should().Be(1234);
        }

        private class BaseEvent : IEvent { }

        private class DerivedEvent : BaseEvent { }

        private class TestEvent : IEvent { }

        private class StringMessage1 : IMessage<string> { }

        private class StringMessage2 : IMessage<string> { }

        private class IntMessage : IMessage<int> { }
    }
}

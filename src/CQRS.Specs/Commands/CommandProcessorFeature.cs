using DX.Contracts;
using DX.Cqrs.Commands;
using DX.Testing;
using FluentAssertions;
using Mongo;
using System;
using System.Collections.Generic;
using System.Threading;
using Xbehave;

namespace Commands {
    [ContractContainer]
    public class CommandProcessorFeature : ContainerFeature {
        [Scenario]
        internal void Scripts(TestServices services, Script script, List<string> log, bool throwException) {
            USING["a test setup"] = () => {
                log = new List<string>();
                throwException = true;
                return services = Setup(c => c
                    .Handle<TestMessage>(m => log.Add(m.Text))
                    .Handle<WaitMessage>(m => {
                        Thread.Sleep(10);
                        log.Add(m.Text);
                    })
                    .Handle<ExceptionMessage>(m => {
                        if (throwException)
                            throw new InvalidOperationException();
                        else
                            log.Add(m.Text);
                    }));
            };

            GIVEN["a script"] = () => script = new ScriptBuilder() {
                Commands = {
                    { ID.NewID(), ID.NewID(), new TestMessage { Text = "M1" } },
                    { ID.NewID(), ID.NewID(), new WaitMessage { Text = "M2" } },
                    { ID.NewID(), ID.NewID(), new TestMessage { Text = "M3" } },
                    { ID.NewID(), ID.NewID(), new TestMessage { Text = "M4" } },
                    { ID.NewID(), ID.NewID(), new ExceptionMessage { Text = "M5" } },
                    { ID.NewID(), ID.NewID(), new TestMessage { Text = "M6" } },
                }
            };

            GIVEN["that two commands were already executed"] = () =>
                services.Execute(
                    ToCommand(script.Commands[0]), 
                    ToCommand(script.Commands[2]));

            WHEN["executing the script"] = () => {
                log.Clear();
                new Action(() => services.Execute(new RunScriptBuilder { Script = script }.Build()))
                    .Should()
                    .Throw<InvalidOperationException>();
            };

            THEN["the unfinished commands are run and the script stops after exception"] = () =>
                log.Should().BeEquivalentTo(new[] { "M2", "M4" }, o => o.WithStrictOrdering());

            WHEN["the script is run again and a previously failed command succeeds"] = () => {
                log.Clear();
                throwException = false;
                services.Execute(new RunScriptBuilder { Script = script }.Build());
            };

            THEN["the remaining commands are executed"] = () =>
                log.Should().BeEquivalentTo("M5", "M6");

        }

        private Command ToCommand(ScriptCommand sc) {
            return new Command(sc.ID, sc.Message, sc.Target);
        }

        [Contract]
        private class TestMessage : ICommandMessage {
            public string Text { get; set; }
        }

        [Contract]
        private class ExceptionMessage : ICommandMessage {
            public string Text { get; set; }
        }

        [Contract]
        private class WaitMessage : ICommandMessage {
            public string Text { get; set; }
        }
    }
}

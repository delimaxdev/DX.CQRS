using FluentAssertions;
using FluentAssertions.Primitives;
using System;

namespace Mongo
{
    internal static class MongoAssertions {
        public static ActionLogAssertions Should(this MongoActionLog log) {
            return new ActionLogAssertions(log);
        }
    }

    internal class ActionLogAssertions :
        ReferenceTypeAssertions<MongoActionLog, ActionLogAssertions> {

        public ActionLogAssertions(MongoActionLog instance) {
            Subject = instance;
        }

        protected override string Identifier => "MongoActionLog";

        public AndConstraint<ActionLogAssertions> Contain(Action<MongoActionLog> actionsBuilder) {
            MongoActionLog exp = new MongoActionLog();
            actionsBuilder(exp);
            foreach (MongoAction a in exp.Actions) {
                Subject.Actions.Should().ContainEquivalentOf(a, o => o.RespectingRuntimeTypes());
            }

            return new AndConstraint<ActionLogAssertions>(this);
        }

        public AndConstraint<ActionLogAssertions> BeExactly(Action<MongoActionLog> actionLogBuilder) {
            MongoActionLog exp = new MongoActionLog();
            actionLogBuilder(exp);
            new ObjectAssertions(Subject).BeEquivalentTo(exp);

            return new AndConstraint<ActionLogAssertions>(this);
        }
    }
}

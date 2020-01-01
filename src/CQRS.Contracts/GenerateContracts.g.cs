#nullable enable
namespace DX.Contracts.Cqrs.Domain
{
    using System;
    using System.Collections.Generic;
    using DX;
    using DX.Contracts;
    using DX.Cqrs.Commands;
    using System;

    partial interface IServerCommand
    {
        [Builder(typeof(CreatedBuilder))]
        partial class Created
        {
            public Created(CreatedBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Target = source.Target;
                Message = source.Message.NotNull();
            }

            public Created(Created source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Target = source.Target;
                Message = source.Message;
            }
        }

        public partial class CreatedBuilder : IBuilds<Created>
        {
            [ContractMember("T")]
            public ID? Target
            {
                get;
                set;
            }

            [ContractMember("Msg")]
            public ICommandMessage? Message
            {
                get;
                set;
            }

            public Created Build()
            {
                return new Created(this);
            }

            public static implicit operator Created(CreatedBuilder builder)
            {
                return builder.Build();
            }

            public CreatedBuilder(Created? source = null): base()
            {
                if (source != null)
                {
                    Target = source.Target;
                    Message = source.Message;
                }
            }

            public CreatedBuilder(): base()
            {
            }
        }

        [Builder(typeof(QueuedBuilder))]
        partial class Queued
        {
            public Queued(QueuedBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp.NotNull();
                Metadata = source.Metadata.NotNull();
            }

            public Queued(Queued source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp;
                Metadata = source.Metadata;
            }
        }

        public partial class QueuedBuilder : IBuilds<Queued>
        {
            [ContractMember("ts")]
            public DateTime? Timestamp
            {
                get;
                set;
            }

            [ContractMember("MD")]
            public CommandMetadata? Metadata
            {
                get;
                set;
            }

            public Queued Build()
            {
                return new Queued(this);
            }

            public static implicit operator Queued(QueuedBuilder builder)
            {
                return builder.Build();
            }

            public QueuedBuilder(Queued? source = null): base()
            {
                if (source != null)
                {
                    Timestamp = source.Timestamp;
                    Metadata = source.Metadata;
                }
            }

            public QueuedBuilder(): base()
            {
            }
        }

        [Builder(typeof(StartedBuilder))]
        partial class Started
        {
            public Started(StartedBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp.NotNull();
            }

            public Started(Started source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp;
            }
        }

        public partial class StartedBuilder : IBuilds<Started>
        {
            [ContractMember("ts")]
            public DateTime? Timestamp
            {
                get;
                set;
            }

            public Started Build()
            {
                return new Started(this);
            }

            public static implicit operator Started(StartedBuilder builder)
            {
                return builder.Build();
            }

            public StartedBuilder(Started? source = null): base()
            {
                if (source != null)
                {
                    Timestamp = source.Timestamp;
                }
            }

            public StartedBuilder(): base()
            {
            }
        }

        [Builder(typeof(SucceededBuilder))]
        partial class Succeeded
        {
            public Succeeded(SucceededBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp.NotNull();
            }

            public Succeeded(Succeeded source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp;
            }
        }

        public partial class SucceededBuilder : IBuilds<Succeeded>
        {
            [ContractMember("ts")]
            public DateTime? Timestamp
            {
                get;
                set;
            }

            public Succeeded Build()
            {
                return new Succeeded(this);
            }

            public static implicit operator Succeeded(SucceededBuilder builder)
            {
                return builder.Build();
            }

            public SucceededBuilder(Succeeded? source = null): base()
            {
                if (source != null)
                {
                    Timestamp = source.Timestamp;
                }
            }

            public SucceededBuilder(): base()
            {
            }
        }

        [Builder(typeof(FailedBuilder))]
        partial class Failed
        {
            public Failed(FailedBuilder source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp.NotNull();
                Message = source.Message.NotNull();
                ExceptionType = source.ExceptionType;
                ExceptionStacktrace = source.ExceptionStacktrace;
            }

            public Failed(Failed source): base()
            {
                if (source == null)
                    throw new ArgumentNullException(nameof(source));
                Timestamp = source.Timestamp;
                Message = source.Message;
                ExceptionType = source.ExceptionType;
                ExceptionStacktrace = source.ExceptionStacktrace;
            }
        }

        public partial class FailedBuilder : IBuilds<Failed>
        {
            [ContractMember("ts")]
            public DateTime? Timestamp
            {
                get;
                set;
            }

            public string? Message
            {
                get;
                set;
            }

            public string? ExceptionType
            {
                get;
                set;
            }

            public string? ExceptionStacktrace
            {
                get;
                set;
            }

            public Failed Build()
            {
                return new Failed(this);
            }

            public static implicit operator Failed(FailedBuilder builder)
            {
                return builder.Build();
            }

            public FailedBuilder(Failed? source = null): base()
            {
                if (source != null)
                {
                    Timestamp = source.Timestamp;
                    Message = source.Message;
                    ExceptionType = source.ExceptionType;
                    ExceptionStacktrace = source.ExceptionStacktrace;
                }
            }

            public FailedBuilder(): base()
            {
            }
        }
    }
}

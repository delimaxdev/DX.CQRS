using DX.Cqrs.Domain;
using System;

namespace Codegen
{
    public partial class SampleClass {
        public interface ISampleInterface {
            /// <summary>
            /// InterfaceStringProp doc.
            /// </summary>
            string InterfaceStringProp { get; }
        }


        /// <summary>
        /// Class comment.
        /// </summary>
        public partial class SampleDO {
            /// <summary>
            /// Property comment.
            /// </summary>
            public string StringProp { get; }

            //public string? NullableStringProp { get; }

            public DateTime DateTimeProp { get; }

            public DateTime? NullableDateTimeProp { get; }

            public Nullable<int> VerboseNullableProp { get; }

            public SampleClass[] ArrayProp { get; }
        }

        public partial class SampleEvent : SampleDO, ISampleInterface {
        }

        public partial class SampleCommand : SampleDO, ISampleInterface, ICauses<SampleEvent> {
            public string AdditionalProperty { get; }
        }
    }
}

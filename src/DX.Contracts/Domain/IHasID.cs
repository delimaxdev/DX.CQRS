using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Contracts {
    public interface IHasID<out TID> where TID : IIdentifier {
        TID ID { get; }
    }
}
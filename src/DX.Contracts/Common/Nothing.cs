using System;
using System.Collections.Generic;
using System.Text;

namespace DX.Cqrs.Commons {
    public class Nothing {
        public static readonly Nothing Value = new Nothing();

        private Nothing() { }
    }
}

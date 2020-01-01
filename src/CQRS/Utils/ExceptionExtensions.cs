using System;
using System.Threading;

namespace DX {
    internal static class ExceptionExtensions {
        public static bool IsCritical(this Exception ex) =>
            ex is AccessViolationException ||
            ex is OutOfMemoryException ||
            ex is StackOverflowException ||
            ex is ThreadAbortException ||
            ex is AppDomainUnloadedException ||
            ex is BadImageFormatException ||
            ex is CannotUnloadAppDomainException ||
            ex is InvalidProgramException;
    }
}
using System;

namespace DX.Cqrs.Commons
{
    public abstract class Disposable : IDisposable {
        protected bool Disposed { get; private set; } = false;

        public void Dispose() {
            if (Disposed)
                return;

            Dispose(true);
            Disposed = true;

            GC.SuppressFinalize(this);
        }

        protected void ThrowIfDisposed() {
            if (Disposed)
                throw new ObjectDisposedException(this.GetType().Name);
        }

        protected abstract void Dispose(bool disposing);
    }
}
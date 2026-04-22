#if NEWTONSOFT_EXISTS
using System;

namespace DingoJsonUI
{
    public sealed class JsonPathSubscription : IDisposable
    {
        private Action _dispose;

        internal JsonPathSubscription(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
#endif

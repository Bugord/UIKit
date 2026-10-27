using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// A ViewModel that produces a single result, e.g. a dialog opened with
    /// <see cref="IScreenNavigator.RunAsync{TViewModel,TResult}"/>.
    /// </summary>
    /// <remarks>
    /// The first <see cref="SetResult"/> wins and is kept, so a result set before anyone waits
    /// is not lost. Each <see cref="WaitForResultAsync"/> call gets its own awaitable, and its
    /// token cancels only that wait. Disposing cancels every wait still pending.
    /// </remarks>
    public class ResultViewModel<TResult> : ViewModel, IResultViewModel<TResult>, IResultState
    {
        private readonly List<Waiter> waiters = new();

        private TResult result;

        public bool HasResult { get; private set; }

        public Awaitable<TResult> WaitForResultAsync(CancellationToken token = default)
        {
            var source = new AwaitableCompletionSource<TResult>();

            if (HasResult) {
                source.SetResult(result);
                return source.Awaitable;
            }

            if (IsDisposed || token.IsCancellationRequested) {
                source.SetCanceled();
                return source.Awaitable;
            }

            var waiter = new Waiter(source);
            waiters.Add(waiter);

            if (token.CanBeCanceled) {
                waiter.Registration = token.Register(() => {
                    if (waiters.Remove(waiter)) {
                        waiter.Source.TrySetCanceled();
                    }
                });
            }

            return source.Awaitable;
        }

        /// <summary>
        /// Completes every pending and future wait with <paramref name="value"/>. Ignored once a
        /// result has been set or the ViewModel has been disposed.
        /// </summary>
        public void SetResult(TResult value)
        {
            if (HasResult || IsDisposed) {
                return;
            }

            HasResult = true;
            result = value;

            foreach (var waiter in TakeWaiters()) {
                waiter.Registration.Dispose();
                waiter.Source.TrySetResult(value);
            }
        }

        private protected override void OnDisposing()
        {
            foreach (var waiter in TakeWaiters()) {
                waiter.Registration.Dispose();
                waiter.Source.TrySetCanceled();
            }
        }

        // Copied out first: completing a waiter runs its continuation synchronously, and that
        // continuation may call back into this ViewModel.
        private Waiter[] TakeWaiters()
        {
            var taken = waiters.ToArray();
            waiters.Clear();
            return taken;
        }

        private sealed class Waiter
        {
            public readonly AwaitableCompletionSource<TResult> Source;
            public CancellationTokenRegistration Registration;

            public Waiter(AwaitableCompletionSource<TResult> source)
            {
                Source = source;
            }
        }
    }

    // Lets the navigator ask whether a result is already set without knowing TResult.
    internal interface IResultState
    {
        bool HasResult { get; }
    }
}

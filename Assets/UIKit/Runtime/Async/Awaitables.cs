using System;
using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// Helpers for <see cref="Awaitable"/>, which Unity 6.0 ships without a completed
    /// instance or a fire-and-forget method.
    /// </summary>
    public static class Awaitables
    {
        /// <summary>
        /// Returns an already completed <see cref="Awaitable"/>, e.g. for a
        /// <see cref="View{TViewModel}.ShowAsync"/> override with nothing to animate.
        /// </summary>
        /// <remarks>
        /// A new instance every call: an Awaitable is pooled and may be awaited only once,
        /// so a shared completed instance would break the second caller.
        /// </remarks>
        public static Awaitable Completed()
        {
            var source = new AwaitableCompletionSource();
            source.SetResult();
            return source.Awaitable;
        }

        public static Awaitable<T> FromResult<T>(T result)
        {
            var source = new AwaitableCompletionSource<T>();
            source.SetResult(result);
            return source.Awaitable;
        }

        /// <summary>
        /// Runs <paramref name="awaitable"/> without awaiting it. Exceptions are logged rather
        /// than lost; cancellation is not an error and is ignored.
        /// </summary>
        public static async void Forget(this Awaitable awaitable)
        {
            try {
                await awaitable;
            } catch (OperationCanceledException) {
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }

        /// <inheritdoc cref="Forget(Awaitable)"/>
        public static async void Forget<T>(this Awaitable<T> awaitable)
        {
            try {
                await awaitable;
            } catch (OperationCanceledException) {
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }
    }
}

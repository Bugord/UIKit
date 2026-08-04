using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UIKit.Runtime.Core
{
    // Serialises navigation transitions. Open/Run/Close each mutate the screen and popup
    // stacks around awaited show/hide animations, so without this two calls started from
    // separate button handlers interleave and the resulting stack order depends on how
    // long each animation happened to take.
    //
    // Unity runs UniTask continuations on the main thread, so plain fields are enough here.
    // SemaphoreSlim would be the obvious primitive but its WaitAsync continuation can
    // resume on a thread pool thread, which is not valid for touching VisualElements.
    //
    // Waiters are served first-in-first-out, so navigation applies in call order.
    internal sealed class NavigationGate
    {
        private readonly Queue<UniTaskCompletionSource> waiters = new();

        private bool held;

        public UniTask WaitAsync()
        {
            if (!held) {
                held = true;
                return UniTask.CompletedTask;
            }

            var waiter = new UniTaskCompletionSource();
            waiters.Enqueue(waiter);

            return waiter.Task;
        }

        public void Release()
        {
            // Ownership passes straight to the next waiter rather than going through
            // held=false, so a call arriving between this release and the waiter's
            // continuation cannot overtake operations that were already queued.
            if (waiters.Count > 0) {
                waiters.Dequeue().TrySetResult();
                return;
            }

            held = false;
        }
    }
}

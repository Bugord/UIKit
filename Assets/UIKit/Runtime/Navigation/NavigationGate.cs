using System;
using System.Collections.Generic;
using UnityEngine;

namespace UIKit
{
    // Serialises navigation operations. Each one mutates the screen and popup stacks around
    // awaited show/hide animations, so two started from separate button handlers would otherwise
    // interleave, and the resulting order would depend on how long each animation took.
    //
    // Waiters are served first-in-first-out, so navigation applies in call order. Everything
    // runs on the main thread, so plain fields are enough.
    //
    // The gate also watches for a stall: an operation queued behind one that has held the gate
    // for too long. The usual cause is a view awaiting a navigator call from its own OnAttach or
    // ShowAsync - a deadlock that otherwise just freezes the UI without a word.
    internal sealed class NavigationGate
    {
        public const float DefaultStallWarningSeconds = 5f;

        private readonly Queue<(AwaitableCompletionSource source, string operation)> waiters = new();
        private readonly Func<double> clock;

        private bool held;
        private string holder;
        private double heldSince;
        private int acquisition;
        private int reportedAcquisition = -1;
        private bool watching;
        private bool stopped;

        public NavigationGate(Func<double> clock = null)
        {
            this.clock = clock ?? (() => Time.realtimeSinceStartupAsDouble);
        }

        /// <summary>
        /// Seconds an operation may hold the gate while another waits before an error is logged.
        /// Zero or less disables the check.
        /// </summary>
        public float StallWarningSeconds { get; set; } = DefaultStallWarningSeconds;

        public bool IsHeld => held;
        public int WaitingCount => waiters.Count;

        public Awaitable WaitAsync(string operation)
        {
            if (!held) {
                Acquire(operation);
                return Awaitables.Completed();
            }

            var source = new AwaitableCompletionSource();
            waiters.Enqueue((source, operation));
            WatchForStall();

            return source.Awaitable;
        }

        public void Release()
        {
            // Ownership passes straight to the next waiter rather than going through held=false,
            // so a call arriving between this release and the waiter's continuation cannot
            // overtake operations that were already queued.
            if (waiters.Count > 0) {
                var (source, operation) = waiters.Dequeue();
                Acquire(operation);
                source.SetResult();
                return;
            }

            held = false;
            holder = null;
        }

        /// <summary>
        /// Stops the stall watchdog for good. Called when the context is disposed: an animation
        /// that ignores its cancellation token could otherwise keep the gate held, and the
        /// watchdog polling, forever.
        /// </summary>
        public void StopWatching()
        {
            stopped = true;
        }

        /// <summary>
        /// Returns a description of the stall if the gate has been held for at least
        /// <see cref="StallWarningSeconds"/> at <paramref name="now"/> with an operation
        /// waiting, or null. Reports each acquisition at most once.
        /// </summary>
        public string CheckStall(double now)
        {
            if (!held || waiters.Count == 0 || StallWarningSeconds <= 0f || reportedAcquisition == acquisition) {
                return null;
            }

            var heldFor = now - heldSince;
            if (heldFor < StallWarningSeconds) {
                return null;
            }

            reportedAcquisition = acquisition;

            return $"UIKit: navigation has been blocked for {heldFor:0.#}s by '{holder}', with " +
                   $"{waiters.Count} operation(s) queued behind it (next: '{waiters.Peek().operation}'). " +
                   "If a view's OnAttach, ShowAsync or HideAsync, or a ViewModel's OnOpened or OnClosed, " +
                   "awaits a navigator call, that is a deadlock: the awaited call cannot start until the " +
                   "current one finishes. Call .Forget() on it instead. If a show/hide animation is simply " +
                   "this long, raise UIKitContext.StallWarningSeconds.";
        }

        private void Acquire(string operation)
        {
            held = true;
            holder = operation;
            heldSince = clock();
            acquisition++;
        }

        private async void WatchForStall()
        {
            if (watching || stopped || StallWarningSeconds <= 0f) {
                return;
            }

            watching = true;

            try {
                while (!stopped && held && waiters.Count > 0) {
                    await Awaitable.NextFrameAsync();

                    var stall = stopped ? null : CheckStall(clock());
                    if (stall != null) {
                        Debug.LogError(stall);
                    }
                }
            } catch (OperationCanceledException) {
            } finally {
                watching = false;
            }
        }
    }
}

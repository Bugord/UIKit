using System;
using UnityEngine;

namespace UIKit
{
    /// <summary>
    /// An <see cref="ICommand"/> that runs an <see cref="Awaitable"/> and cannot execute again
    /// until it finishes, so a double click does not start it twice.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="CanExecute"/> is false while the command runs, while another command on the same
    /// <see cref="CommandGate"/> runs, or while the predicate returns false.
    /// <see cref="ICommand.CanExecuteChanged"/> is raised when it starts and when it finishes,
    /// whether it completes, throws or is cancelled. For a change of the predicate's inputs, call
    /// <see cref="RaiseCanExecuteChanged"/>.
    /// </para>
    /// <para>
    /// <see cref="IsRunning"/> is a plain property: a UXML binding to it would not refresh, because
    /// bindings listen to the ViewModel's changes, not the command's. A ViewModel that wants to show
    /// it exposes its own property instead.
    /// </para>
    /// </remarks>
    public sealed class AsyncCommand : ICommand
    {
        private readonly Func<Awaitable> execute;
        private readonly Func<bool> canExecute;
        private readonly CommandGate gate;

        public event Action CanExecuteChanged;

        public bool IsRunning { get; private set; }

        public bool CanExecute => !IsRunning && (gate == null || !gate.IsBusy) && (canExecute == null || canExecute());

        /// <param name="execute">What the command does, e.g. a navigation awaited to its end.</param>
        /// <param name="canExecute">Whether it can run, besides not running already; <c>null</c> means always.</param>
        /// <param name="gate">Shared with the commands it must not overlap with; <c>null</c> for none.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute"/> is null.</exception>
        public AsyncCommand(Func<Awaitable> execute, Func<bool> canExecute = null, CommandGate gate = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
            this.gate = gate;
            gate?.Add(this);
        }

        /// <inheritdoc cref="AsyncCommand(Func{Awaitable}, Func{bool}, CommandGate)"/>
        public AsyncCommand(Func<Awaitable> execute, CommandGate gate) : this(execute, null, gate)
        {
        }

        /// <summary>
        /// Runs the command without waiting for it, logging a failure as
        /// <see cref="Awaitables.Forget(Awaitable)"/> does. Ignored while it cannot execute.
        /// </summary>
        public void Execute() => ExecuteAsync().Forget();

        /// <summary>
        /// Runs the command and completes when it finishes, with its exception if it fails.
        /// While it cannot execute, the call is ignored and an already completed awaitable is
        /// returned.
        /// </summary>
        public Awaitable ExecuteAsync() => CanExecute ? RunAsync() : Awaitables.Completed();

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke();

        // Everything before the first await runs synchronously within the click, so a second
        // click in the same frame already sees the command (and its gate) running.
        private async Awaitable RunAsync()
        {
            IsRunning = true;

            try {
                if (gate != null) {
                    gate.Enter();
                } else {
                    RaiseCanExecuteChanged();
                }

                await execute();
            } finally {
                IsRunning = false;

                if (gate != null) {
                    gate.Exit();
                } else {
                    RaiseCanExecuteChanged();
                }
            }
        }
    }
}

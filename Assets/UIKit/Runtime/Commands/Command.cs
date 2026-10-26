using System;

namespace UIKit
{
    /// <summary>
    /// A synchronous <see cref="ICommand"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="CanExecute"/> is read from the predicate each time, but nothing watches what the
    /// predicate depends on: call <see cref="RaiseCanExecuteChanged"/> when that changes, so a
    /// bound button updates.
    /// </remarks>
    public sealed class Command : ICommand
    {
        private readonly Action execute;
        private readonly Func<bool> canExecute;

        public event Action CanExecuteChanged;

        public bool CanExecute => canExecute == null || canExecute();

        /// <param name="execute">What the command does.</param>
        /// <param name="canExecute">Whether it can run; <c>null</c> means always.</param>
        /// <exception cref="ArgumentNullException"><paramref name="execute"/> is null.</exception>
        public Command(Action execute, Func<bool> canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        public void Execute()
        {
            if (CanExecute) {
                execute();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke();
    }
}

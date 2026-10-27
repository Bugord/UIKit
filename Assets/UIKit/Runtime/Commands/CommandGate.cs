using System.Collections.Generic;

namespace UIKit
{
    /// <summary>
    /// Makes the <see cref="AsyncCommand"/>s created with it mutually exclusive: while one of
    /// them runs, none of them can execute.
    /// </summary>
    /// <remarks>
    /// For a group of buttons that must not overlap, e.g. menu buttons that each open a screen:
    /// two of them clicked in the same frame would otherwise both navigate. Every command on the
    /// gate raises <see cref="ICommand.CanExecuteChanged"/> when it opens or closes, so their
    /// buttons are disabled together.
    /// </remarks>
    public sealed class CommandGate
    {
        private readonly List<AsyncCommand> commands = new();

        /// <summary>
        /// Whether one of the gate's commands is running.
        /// </summary>
        public bool IsBusy { get; private set; }

        internal void Add(AsyncCommand command) => commands.Add(command);

        internal void Enter()
        {
            IsBusy = true;
            RaiseCanExecuteChanged();
        }

        internal void Exit()
        {
            IsBusy = false;
            RaiseCanExecuteChanged();
        }

        // A copy, so a handler that creates another command on this gate does not break the loop.
        private void RaiseCanExecuteChanged()
        {
            foreach (var command in commands.ToArray()) {
                command.RaiseCanExecuteChanged();
            }
        }
    }
}

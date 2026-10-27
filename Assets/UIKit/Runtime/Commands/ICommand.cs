using System;

namespace UIKit
{
    /// <summary>
    /// An action a ViewModel exposes to its view, together with whether it can run right now.
    /// Bind it to a button with <c>View.BindCommand</c>, which enables the button only while
    /// <see cref="CanExecute"/> is true.
    /// </summary>
    public interface ICommand
    {
        bool CanExecute { get; }

        /// <summary>
        /// Raised when <see cref="CanExecute"/> may have changed.
        /// </summary>
        event Action CanExecuteChanged;

        /// <summary>
        /// Runs the command. Ignored while <see cref="CanExecute"/> is false.
        /// </summary>
        void Execute();
    }
}

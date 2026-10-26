using System;

namespace UIKit
{
    /// <summary>
    /// User input that dismisses the top screen or popup. Dismissing closes it the same way
    /// <see cref="IScreenNavigator.CloseAsync"/> does, so an <see cref="ICancelable"/>
    /// ViewModel is cancelled first.
    /// </summary>
    [Flags]
    public enum DismissTriggers
    {
        None = 0,

        /// <summary>
        /// A pointer press outside the popup's content: on its root element, which is meant to
        /// be the backdrop, or anywhere outside it. Applies to popups only.
        /// </summary>
        BackdropClick = 1 << 0,

        /// <summary>
        /// Escape or the gamepad cancel button. A screen is never dismissed this way while it
        /// is the bottom of the stack.
        /// </summary>
        NavigationCancel = 1 << 1,

        All = BackdropClick | NavigationCancel
    }
}

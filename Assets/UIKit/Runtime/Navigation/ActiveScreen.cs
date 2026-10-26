using System.Threading;

namespace UIKit
{
    // One screen or popup the navigator is tracking, from the moment it starts opening.
    internal sealed class ActiveScreen
    {
        public readonly ViewModel ViewModel;
        public readonly IView View;
        public readonly ScreenGroup Owner;
        public readonly ScreenInfo Info;

        // Set before a hide animation starts, so nothing tries to hide it twice and the
        // navigator knows the top screen has to be shown again once it is uncovered.
        public bool Hidden;

        private readonly CancellationTokenSource lifetime = new();

        // Set when its group or context is disposed while it is still opening, open or closing:
        // an open aborts instead of pushing a disposed view, and a running animation is cancelled.
        public bool IsDead { get; private set; }

        public CancellationToken Lifetime => lifetime.Token;

        public ActiveScreen(ViewModel viewModel, IView view, ScreenGroup owner, ScreenInfo info)
        {
            ViewModel = viewModel;
            View = view;
            Owner = owner;
            Info = info;
        }

        public void Kill()
        {
            if (IsDead) {
                return;
            }

            IsDead = true;
            lifetime.Cancel();
        }
    }
}

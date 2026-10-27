using System;

namespace UIKit
{
    // How one screen is built, and the group it belongs to. The factories close over whatever
    // the screen was registered with - typically a DI container scope - so the ViewModel can
    // take that scope's services. Owner is what lets the screen be evicted with its group.
    // CreateViewModel is null for a child-only view, which cannot be opened as a screen.
    internal sealed class ScreenBinding
    {
        public readonly Func<ViewModel> CreateViewModel;
        public readonly Func<IView> CreateView;
        public readonly ScreenGroup Owner;

        public ScreenBinding(Func<ViewModel> createViewModel, Func<IView> createView, ScreenGroup owner)
        {
            CreateViewModel = createViewModel;
            CreateView = createView;
            Owner = owner;
        }
    }
}

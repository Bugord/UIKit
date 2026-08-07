using System;
using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    /// <summary>
    /// How one screen is built, and which scope it belongs to.
    /// </summary>
    /// <remarks>
    /// The factories close over the container the screen was registered in, which is what
    /// lets a screen registered in a child scope resolve its ViewModel from there - so the
    /// ViewModel can take that scope's services rather than only the ones visible to
    /// whichever scope installed UIKit. <see cref="Owner"/> is an opaque token for that
    /// scope; it is what makes eviction possible once the scope goes away.
    /// </remarks>
    public sealed class ScreenBinding
    {
        public readonly Func<ViewModel> CreateViewModel;
        public readonly Func<IView> CreateView;
        public readonly object Owner;

        public ScreenBinding(Func<ViewModel> createViewModel, Func<IView> createView, object owner)
        {
            CreateViewModel = createViewModel;
            CreateView = createView;
            Owner = owner;
        }
    }
}

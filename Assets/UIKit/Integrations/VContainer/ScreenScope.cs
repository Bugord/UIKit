using System;

namespace UIKit.VContainer
{
    /// <summary>
    /// The <see cref="ScreenGroup"/> of one <c>LifetimeScope</c>. One instance is created per
    /// scope that calls <see cref="ContainerBuilderExtensions.RegisterScreen{TViewModel,TView}"/>,
    /// and VContainer disposes it together with that scope, which closes and unregisters the
    /// scope's screens.
    /// </summary>
    public sealed class ScreenScope : IDisposable
    {
        public ScreenScope(UIKitContext context)
        {
            Group = context.CreateGroup();
        }

        public ScreenGroup Group { get; }

        public void Dispose() => Group.Dispose();
    }
}

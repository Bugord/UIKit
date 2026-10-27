using System;
using System.Collections.Generic;
using Zenject;

namespace UIKit.Zenject
{
    /// <summary>
    /// The <see cref="ScreenGroup"/> of one container. One instance is created per container
    /// that calls <see cref="DiContainerExtensions.RegisterScreen{TViewModel,TView}"/>, and it is
    /// disposed with that container, which closes and unregisters the container's screens.
    /// </summary>
    public sealed class ScreenScope : IDisposable
    {
        // InjectLocal: a subcontainer must register only its own screens, not its parent's again.
        public ScreenScope(UIKitContext context, [InjectLocal] List<ScreenRegistration> registrations,
            DiContainer container)
        {
            Group = context.CreateGroup();

            foreach (var registration in registrations) {
                // The container the scope was built in, so ViewModels resolve its bindings.
                registration.Register(Group, container);
            }
        }

        public ScreenGroup Group { get; }

        public void Dispose() => Group.Dispose();
    }

    /// <summary>
    /// One <see cref="DiContainerExtensions.RegisterScreen{TViewModel,TView}"/> call, waiting for
    /// its container's <see cref="ScreenScope"/>.
    /// </summary>
    public sealed class ScreenRegistration
    {
        internal readonly Action<ScreenGroup, DiContainer> Register;

        internal ScreenRegistration(Action<ScreenGroup, DiContainer> register)
        {
            Register = register;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// The UIKit entry point: owns the navigator, the view factory and the screen registrations.
    /// Create one for the lifetime of your UI and dispose it when the UI goes away.
    /// </summary>
    /// <remarks>
    /// DI integrations create and dispose it for you. Without DI:
    /// <code>
    /// var ui = new UIKitContext(panelSettings, catalog);
    /// ui.Register&lt;MainMenuViewModel, MainMenuView&gt;(() =&gt; new MainMenuViewModel(ui.Navigator));
    /// await ui.Navigator.OpenAsync&lt;MainMenuViewModel&gt;();
    /// </code>
    /// </remarks>
    public sealed class UIKitContext : IDisposable
    {
        private readonly ScreenRegistry screenRegistry = new();
        private readonly NavigationGate gate = new();
        private readonly ScreenNavigator screenNavigator;
        private readonly ViewFactory viewFactory;
        private readonly List<ScreenGroup> groups = new();
        private readonly IDisposable ownedRootProvider;

        private ScreenGroup defaultGroup;

        /// <summary>
        /// Creates a context that hosts its UI in a new panel rendered with
        /// <paramref name="panelSettings"/>. The panel is destroyed with the context.
        /// </summary>
        public UIKitContext(PanelSettings panelSettings, params ScreenCatalog[] catalogs)
            : this(new UIRootProvider(panelSettings), catalogs, true)
        {
        }

        /// <summary>
        /// Creates a context that hosts its UI wherever <paramref name="rootProvider"/> says,
        /// for example a <see cref="VisualElementRootProvider"/> inside your own UIDocument.
        /// </summary>
        public UIKitContext(IUIRootProvider rootProvider, params ScreenCatalog[] catalogs)
            : this(rootProvider, catalogs, false)
        {
        }

        private UIKitContext(IUIRootProvider rootProvider, ScreenCatalog[] catalogs, bool ownsRootProvider)
        {
            if (rootProvider == null) {
                throw new ArgumentNullException(nameof(rootProvider));
            }

            if (ownsRootProvider) {
                ownedRootProvider = rootProvider as IDisposable;
            }

            viewFactory = new ViewFactory(screenRegistry, new CatalogSet(catalogs ?? Array.Empty<ScreenCatalog>()));
            screenNavigator = new ScreenNavigator(screenRegistry, viewFactory, rootProvider, gate);
        }

        public IScreenNavigator Navigator => screenNavigator;

        /// <summary>
        /// Creates views for screens that are not opened through the navigator, such as a HUD
        /// widget. Views created here are not tracked: dispose them yourself.
        /// </summary>
        public IViewFactory ViewFactory => viewFactory;

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Seconds a navigation operation may block a queued one before UIKit logs an error
        /// about a likely deadlock. Zero or less disables the check.
        /// </summary>
        public float StallWarningSeconds
        {
            get => gate.StallWarningSeconds;
            set => gate.StallWarningSeconds = value;
        }

        /// <summary>
        /// Creates a group of screens that can be unregistered - and their open screens closed -
        /// together, by disposing it.
        /// </summary>
        public ScreenGroup CreateGroup()
        {
            ThrowIfDisposed();

            var group = new ScreenGroup(screenRegistry, screenNavigator);
            groups.Add(group);
            return group;
        }

        /// <summary>
        /// Registers a screen for the lifetime of the context. Both factories are called each
        /// time the screen opens.
        /// </summary>
        public UIKitContext Register<TViewModel, TView>(Func<TViewModel> createViewModel, Func<TView> createView)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            DefaultGroup().Register(createViewModel, createView);
            return this;
        }

        /// <summary>
        /// Registers a screen whose view has a parameterless constructor.
        /// </summary>
        public UIKitContext Register<TViewModel, TView>(Func<TViewModel> createViewModel)
            where TViewModel : ViewModel
            where TView : View<TViewModel>, new()
        {
            DefaultGroup().Register<TViewModel, TView>(createViewModel);
            return this;
        }

        /// <summary>
        /// Registers a view that is only ever created as a child of another view; see
        /// <see cref="ScreenGroup.RegisterView{TViewModel,TView}(Func{TView})"/>.
        /// </summary>
        public UIKitContext RegisterView<TViewModel, TView>(Func<TView> createView)
            where TViewModel : ViewModel
            where TView : View<TViewModel>
        {
            DefaultGroup().RegisterView<TViewModel, TView>(createView);
            return this;
        }

        /// <summary>
        /// Registers a child view that has a parameterless constructor.
        /// </summary>
        public UIKitContext RegisterView<TViewModel, TView>()
            where TViewModel : ViewModel
            where TView : View<TViewModel>, new()
        {
            DefaultGroup().RegisterView<TViewModel, TView>();
            return this;
        }

        /// <summary>
        /// Disposes every open screen without hide animations, unregisters all screens and,
        /// if the context created its panel, destroys it.
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) {
                return;
            }

            IsDisposed = true;

            screenNavigator.DisposeAll();

            foreach (var group in groups) {
                group.Dispose();
            }

            groups.Clear();
            ownedRootProvider?.Dispose();
        }

        private ScreenGroup DefaultGroup()
        {
            ThrowIfDisposed();
            return defaultGroup ??= CreateGroup();
        }

        private void ThrowIfDisposed()
        {
            if (IsDisposed) {
                throw new ObjectDisposedException(nameof(UIKitContext));
            }
        }
    }
}

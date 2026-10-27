using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace UIKit.Tests.PlayMode
{
    internal sealed class HomeViewModel : ViewModel
    {
    }

    internal sealed class DetailsViewModel : ViewModel
    {
    }

    [UIScreen(Dismiss = DismissTriggers.None)]
    internal sealed class PinnedViewModel : ViewModel
    {
    }

    internal sealed class NoticeViewModel : ViewModel, IPopup
    {
    }

    internal sealed class ToastViewModel : ViewModel, IPopup
    {
    }

    [UIScreen(Dismiss = DismissTriggers.None)]
    internal sealed class LockedNoticeViewModel : ViewModel, IPopup
    {
    }

    internal sealed class QuestionViewModel : PopupViewModel<bool>
    {
        protected override bool CancelResult => false;
    }

    internal sealed class SlowViewModel : ViewModel
    {
    }

    internal sealed class StubbornViewModel : ViewModel
    {
    }

    /// <summary>
    /// A view built in code: a root named after the screen id holding a focusable "content"
    /// element, the part of a popup that is not backdrop.
    /// </summary>
    internal sealed class PlayView<TViewModel> : View<TViewModel> where TViewModel : ViewModel
    {
        private readonly AwaitableCompletionSource showHold;
        private readonly bool ignoreCancellation;

        public PlayView()
        {
        }

        // With ignoreCancellation, the show behaves like an animation that never checks its
        // token: only completing showHold ends it.
        public PlayView(AwaitableCompletionSource showHold, bool ignoreCancellation = false)
        {
            this.showHold = showHold;
            this.ignoreCancellation = ignoreCancellation;
        }

        public VisualElement Content { get; private set; }

        public override Awaitable ShowAsync(CancellationToken token)
        {
            return showHold == null ? base.ShowAsync(token) : HeldShowAsync(token);
        }

        protected override VisualElement CreateRoot()
        {
            var root = new VisualElement { name = ScreenInfo.Of(typeof(TViewModel)).Id };
            Content = new VisualElement { name = "content", focusable = true };
            root.Add(Content);
            return root;
        }

        private async Awaitable HeldShowAsync(CancellationToken token)
        {
            if (ignoreCancellation) {
                await showHold.Awaitable;
                return;
            }

            using (token.Register(() => showHold.TrySetCanceled())) {
                await showHold.Awaitable;
            }
        }
    }

    /// <summary>
    /// A PanelSettings with an (empty) theme, so the panel does not warn about a missing one.
    /// </summary>
    internal sealed class TestPanelSettings : IDisposable
    {
        private readonly ThemeStyleSheet theme;

        public TestPanelSettings()
        {
            theme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
            Settings = ScriptableObject.CreateInstance<PanelSettings>();
            Settings.themeStyleSheet = theme;
        }

        public PanelSettings Settings { get; }

        public void Dispose()
        {
            Object.Destroy(Settings);
            Object.Destroy(theme);
        }
    }

    /// <summary>
    /// A UIDocument on a real runtime panel hosting a <see cref="UIKitContext"/>, for what needs
    /// a panel: focus and event dispatch.
    /// </summary>
    public abstract class PanelTestBase
    {
        private const int MaxFrames = 30;

        private readonly List<IView> views = new();
        private TestPanelSettings panelSettings;
        private GameObject host;

        private protected UIKitContext Context { get; private set; }
        private protected IScreenNavigator Navigator => Context.Navigator;
        private protected VisualElement ScreenLayer { get; private set; }
        private protected VisualElement PopupLayer { get; private set; }
        private protected Focusable Focused => ScreenLayer.panel?.focusController.focusedElement;

        [UnitySetUp]
        public IEnumerator SetUpPanel()
        {
            panelSettings = new TestPanelSettings();

            host = new GameObject("UIKitTestHost");
            host.SetActive(false);
            var document = host.AddComponent<UIDocument>();
            document.panelSettings = panelSettings.Settings;
            host.SetActive(true);

            for (var frame = 0; document.rootVisualElement?.panel == null; frame++) {
                Assert.That(frame, Is.LessThan(MaxFrames), "The UIDocument never got a panel.");
                yield return null;
            }

            var provider = new VisualElementRootProvider(document.rootVisualElement);
            ScreenLayer = provider.GetLayerRoot(UILayer.Screen);
            PopupLayer = provider.GetLayerRoot(UILayer.Popup);
            Context = new UIKitContext(provider) { StallWarningSeconds = 0 };
            RegisterScreens();
        }

        // Runs at the end of the panel set-up, so the context exists.
        private protected virtual void RegisterScreens()
        {
        }

        [TearDown]
        public void TearDownPanel()
        {
            Context?.Dispose();
            Object.Destroy(host);
            panelSettings?.Dispose();
            views.Clear();
        }

        private protected void Register<TViewModel>() where TViewModel : ViewModel, new()
        {
            Context.Register(() => new TViewModel(), () => Track(new PlayView<TViewModel>()));
        }

        private protected void RegisterHeld<TViewModel>(AwaitableCompletionSource showHold)
            where TViewModel : ViewModel, new()
        {
            Context.Register(() => new TViewModel(), () => Track(new PlayView<TViewModel>(showHold)));
        }

        private protected TViewModel Open<TViewModel>() where TViewModel : ViewModel =>
            Sync.Result(Navigator.OpenAsync<TViewModel>()).ViewModel;

        private protected PlayView<TViewModel> ViewOf<TViewModel>(TViewModel viewModel)
            where TViewModel : ViewModel
        {
            foreach (var view in views) {
                if (ReferenceEquals(view.ViewModel, viewModel)) {
                    return (PlayView<TViewModel>)view;
                }
            }

            throw new InvalidOperationException($"No view was created for this {typeof(TViewModel).Name}.");
        }

        private protected static void PointerDown(VisualElement target)
        {
            using var evt = PointerDownEvent.GetPooled();
            evt.target = target;
            target.SendEvent(evt);
        }

        // Navigation events go to the focused element, so the target is focused first, as the
        // navigator does with the top screen.
        private protected static void NavigationCancel(VisualElement target)
        {
            target.Focus();

            using var evt = NavigationCancelEvent.GetPooled();
            evt.target = target;
            target.SendEvent(evt);
        }

        // Closing from an event handler is fire-and-forget; with views that finish at once it
        // ends within the dispatch, but a few frames are allowed for it.
        private protected static IEnumerator Until(Func<bool> condition)
        {
            for (var frame = 0; frame < MaxFrames && !condition(); frame++) {
                yield return null;
            }
        }

        private protected static IEnumerator Frames(int count)
        {
            for (var frame = 0; frame < count; frame++) {
                yield return null;
            }
        }

        private PlayView<TViewModel> Track<TViewModel>(PlayView<TViewModel> view) where TViewModel : ViewModel
        {
            views.Add(view);
            return view;
        }
    }

    internal static class Sync
    {
        public static T Result<T>(Awaitable<T> awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Expected the operation to complete synchronously.");
            return awaiter.GetResult();
        }

        public static void Complete(Awaitable awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True, "Expected the operation to complete synchronously.");
            awaiter.GetResult();
        }
    }
}

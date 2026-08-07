using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    /// <summary>
    /// Opens, closes and stacks screens and popups. Transitions are serialised, so calls
    /// made while another transition is running queue up and apply in call order.
    /// </summary>
    /// <remarks>
    /// A transition runs your <see cref="View{TViewModel}.OnAttach"/>, ShowAsync and
    /// HideAsync while the navigation queue is held, so do not <c>await</c> another
    /// navigator call from inside them - it would wait on an operation that cannot start
    /// until the current one finishes. Call <c>.Forget()</c> to queue it instead.
    /// </remarks>
    public class ScreenNavigator : IScreenNavigator
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly IViewFactory viewFactory;
        private readonly IUIRootProvider uiRootProvider;
        private readonly ScreenCatalog screensCatalog;

        private readonly List<ActiveView> activeScreens = new();
        private readonly List<ActiveView> screenStack = new();
        private readonly List<ActiveView> popupStack = new();
        private readonly NavigationGate gate = new();

        private ActiveView topInteractive;

        public ScreenNavigator(ScreenRegistry screenRegistry, IViewFactory viewFactory,
            IUIRootProvider uiRootProvider, ScreenCatalog screensCatalog)
        {
            this.screenRegistry = screenRegistry;
            this.viewFactory = viewFactory;
            this.screensCatalog = screensCatalog;
            this.uiRootProvider = uiRootProvider;
        }

        public async UniTask<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>
        {
            var activeView = await PushExclusiveAsync<TViewModel>(null, token);

            try {
                // Deliberately outside the gate - this waits on the user, and holding the
                // gate here would stall every other navigation until the view closes.
                return await ((TViewModel)activeView.ViewModel).WaitForResultAsync(token);
            } finally {
                // CancellationToken.None so a cancelled wait still plays the hide
                // animation rather than snapping the view away mid-transition.
                await CloseExclusiveAsync(activeView, CancellationToken.None);
            }
        }

        public async UniTask<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>, IPayloadReceiver<TPayload>
        {
            var activeView = await PushExclusiveAsync<TViewModel>(vm => vm.SetPayload(payload), token);

            try {
                return await ((TViewModel)activeView.ViewModel).WaitForResultAsync(token);
            } finally {
                await CloseExclusiveAsync(activeView, CancellationToken.None);
            }
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var activeView = await PushExclusiveAsync<TViewModel>(null, token);

            return new ScreenHandle<TViewModel>(this, (TViewModel)activeView.ViewModel);
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            var activeView = await PushExclusiveAsync<TViewModel>(vm => vm.SetPayload(payload), token);

            return new ScreenHandle<TViewModel>(this, (TViewModel)activeView.ViewModel);
        }

        public async UniTask CloseAsync<TViewModel>(TViewModel viewModel, CancellationToken token = default)
            where TViewModel : ViewModel
        {
            await gate.WaitAsync();

            try {
                // Looked up under the gate so the view cannot be closed by another
                // operation between finding it here and starting its transition.
                var activeView = activeScreens.Find(x => x.ViewModel == viewModel);

                if (activeView != null) {
                    await CloseInternal(activeView, token);
                }
            } finally {
                gate.Release();
            }
        }

        public bool IsOpen<TViewModel>(TViewModel viewModel)
            where TViewModel : ViewModel
        {
            return activeScreens.Any(screen => screen.ViewModel == viewModel);
        }

        /// <summary>
        /// Force-closes every screen that was opened from <paramref name="owner"/>'s scope,
        /// without playing hide animations. Called as that scope is disposed.
        /// </summary>
        /// <remarks>
        /// Synchronous and ungated, unlike every other close. By the time a scope is being
        /// disposed the services its ViewModels hold are already going away, so awaiting a
        /// hide animation would run view code against a half-torn-down scope - and scope
        /// disposal gives us nowhere to await from in any case. For the same reason this does
        /// not take the navigation gate: a transition still in flight when its own scope dies
        /// cannot be salvaged, and blocking teardown on it would only strand the UI.
        /// </remarks>
        public void DisposeScreensOwnedBy(object owner)
        {
            var previousTopScreen = screenStack.Count > 0 ? screenStack[^1] : null;

            for (var i = activeScreens.Count - 1; i >= 0; i--) {
                var activeView = activeScreens[i];

                if (!ReferenceEquals(activeView.Owner, owner)) {
                    continue;
                }

                activeScreens.RemoveAt(i);
                screenStack.Remove(activeView);
                popupStack.Remove(activeView);
                activeView.View.Dispose();
            }

            var topScreen = screenStack.Count > 0 ? screenStack[^1] : null;

            // Whatever is now top of the screen stack was hidden when the screen above it
            // opened, so it has to be shown again - same as an ordinary close would.
            if (topScreen != null && topScreen != previousTopScreen) {
                topScreen.View.ShowAsync(CancellationToken.None).Forget();
            }

            SetTopInteractive(CurrentTop());
        }

        // Resolving, attaching and pushing all touch the shared stacks, and the pushes
        // themselves await animations, so the whole sequence has to run under the gate.
        private async UniTask<ActiveView> PushExclusiveAsync<TViewModel>(Action<TViewModel> beforeAttach,
            CancellationToken token)
            where TViewModel : ViewModel
        {
            await gate.WaitAsync();

            try {
                var activeView = ResolveViewInternal(out var layer, out var popupOptions, beforeAttach);

                await PushAsync(activeView, layer, popupOptions, token);

                return activeView;
            } finally {
                gate.Release();
            }
        }

        private async UniTask CloseExclusiveAsync(ActiveView activeView, CancellationToken token)
        {
            await gate.WaitAsync();

            try {
                await CloseInternal(activeView, token);
            } finally {
                gate.Release();
            }
        }

        private ActiveView ResolveViewInternal<TViewModel>(out UILayer layer, out PopupOptions popupOptions,
            Action<TViewModel> beforeAttach = null)
            where TViewModel : ViewModel
        {
            var binding = screenRegistry.Get(typeof(TViewModel));
            var viewModel = (TViewModel)binding.CreateViewModel();
            beforeAttach?.Invoke(viewModel);

            var entry = screensCatalog.Get(UIId.OfScreen(typeof(TViewModel)));
            layer = entry.layer;
            popupOptions = entry.popupOptions.ToOptions();
            var container = uiRootProvider.GetLayerRoot(layer);

            var view = viewFactory.Create<View<TViewModel>, TViewModel>(viewModel, container);

            // The binding's owner rides along so this screen can be closed with the scope it
            // came from, whether or not that is the scope the navigator itself lives in.
            return new ActiveView(viewModel, view, binding.Owner);
        }

        private async UniTask PushAsync(ActiveView activeView, UILayer layer, PopupOptions popupOptions,
            CancellationToken token)
        {
            activeScreens.Add(activeView);

            try {
                if (layer == UILayer.Screen) {
                    if (screenStack.Count > 0) {
                        await screenStack[^1].View.HideAsync(token);
                    }

                    screenStack.Add(activeView);
                }

                await activeView.View.ShowAsync(token);

                if (layer == UILayer.Popup) {
                    popupStack.Add(activeView);
                    RegisterPopupCancelBehaviour(activeView, popupOptions);
                }

                SetTopInteractive(CurrentTop());
            } catch {
                // A failed/cancelled push (e.g. mid Show/HideAsync) must not leave the
                // view attached and untracked, or the previous top screen stuck hidden.
                activeScreens.Remove(activeView);
                screenStack.Remove(activeView);
                popupStack.Remove(activeView);
                activeView.View.Dispose();

                if (layer == UILayer.Screen && screenStack.Count > 0) {
                    await screenStack[^1].View.ShowAsync(CancellationToken.None);
                }

                throw;
            }
        }

        private static void RegisterPopupCancelBehaviour(ActiveView activeView, PopupOptions popupOptions)
        {
            if (activeView.ViewModel is not ICancelable cancelable) {
                return;
            }

            var root = activeView.View.Root;

            if (popupOptions.CancelOnBackdropClick) {
                root.RegisterCallback<PointerDownEvent>(evt => {
                    if (evt.target == root) {
                        cancelable.Cancel();
                    }
                });
            }

            if (popupOptions.CancelOnNavigationCancel) {
                // Navigation events (Escape / gamepad cancel) only target the currently
                // focused element; SetTopInteractive grants focus once this popup is on top.
                root.focusable = true;
                root.delegatesFocus = true;

                root.RegisterCallback<NavigationCancelEvent>(evt => {
                    cancelable.Cancel();
                    evt.StopPropagation();
                });
            }
        }

        private ActiveView CurrentTop() =>
            popupStack.Count > 0 ? popupStack[^1]
            : screenStack.Count > 0 ? screenStack[^1]
            : null;

        // Only the top-most screen/popup should be reachable via keyboard/gamepad
        // navigation; popups always render above screens, so they take priority.
        private void SetTopInteractive(ActiveView next)
        {
            if (topInteractive == next) {
                return;
            }

            topInteractive?.View.Root?.SetEnabled(false);
            topInteractive = next;
            topInteractive?.View.Root?.SetEnabled(true);
            topInteractive?.View.Root?.Focus();
        }

        private async UniTask CloseInternal(ActiveView activeView, CancellationToken token)
        {
            // Untrack synchronously: a second close for the same view can arrive while the
            // hide animation below is still awaiting, and would otherwise dispose it twice.
            if (!activeScreens.Remove(activeView)) {
                return;
            }

            try {
                await activeView.View.HideAsync(token);
            } finally {
                await DetachAsync(activeView);
            }
        }

        // Detaching is cleanup, so it takes no token and must complete even when the hide
        // animation above was cancelled - otherwise the view stays attached and undisposed,
        // and the screen underneath stays hidden forever.
        private async UniTask DetachAsync(ActiveView activeView)
        {
            activeView.View.Dispose();

            popupStack.Remove(activeView);

            var stackIndex = screenStack.IndexOf(activeView);
            if (stackIndex >= 0) {
                var wasTop = stackIndex == screenStack.Count - 1;
                screenStack.RemoveAt(stackIndex);

                if (wasTop && screenStack.Count > 0) {
                    await screenStack[^1].View.ShowAsync(CancellationToken.None);
                }
            }

            SetTopInteractive(CurrentTop());
        }
    }
}

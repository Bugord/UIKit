using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public class ScreenNavigator : IScreenNavigator
    {
        private readonly IScreenResolver screenResolver;
        private readonly IViewFactory viewFactory;
        private readonly IUIRootProvider uiRootProvider;
        private readonly ScreenCatalog screensCatalog;

        private readonly List<ActiveView> activeScreens = new();
        private readonly List<ActiveView> screenStack = new();
        private readonly List<ActiveView> popupStack = new();

        private ActiveView topInteractive;

        public ScreenNavigator(IScreenResolver screenResolver, IViewFactory viewFactory,
            IUIRootProvider uiRootProvider, ScreenCatalog screensCatalog)
        {
            this.screenResolver = screenResolver;
            this.viewFactory = viewFactory;
            this.screensCatalog = screensCatalog;
            this.uiRootProvider = uiRootProvider;
        }

        public async UniTask<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, out var popupOptions);
            var activeView = new ActiveView(view.ViewModel, view);

            await PushAsync(activeView, layer, popupOptions, token);

            try {
                return await view.ViewModel.WaitForResultAsync(token);
            } finally {
                // Cleanup must run even if the wait was cancelled, so use a token that
                // can't itself be cancelled - otherwise a cancelled RunAsync leaves the
                // view visible and never removed from the active/popup stacks.
                await CloseInternal(activeView, CancellationToken.None);
            }
        }

        public async UniTask<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, out var popupOptions,
                vm => vm.SetPayload(payload));
            var activeView = new ActiveView(view.ViewModel, view);

            await PushAsync(activeView, layer, popupOptions, token);

            try {
                return await view.ViewModel.WaitForResultAsync(token);
            } finally {
                await CloseInternal(activeView, CancellationToken.None);
            }
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, out var popupOptions);
            var activeView = new ActiveView(view.ViewModel, view);

            await PushAsync(activeView, layer, popupOptions, token);

            return new ScreenHandle<TViewModel>(this, view.ViewModel);
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, out var popupOptions,
                vm => vm.SetPayload(payload));
            var activeView = new ActiveView(view.ViewModel, view);

            await PushAsync(activeView, layer, popupOptions, token);

            return new ScreenHandle<TViewModel>(this, view.ViewModel);
        }

        public UniTask CloseAsync<TViewModel>(TViewModel viewModel, CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var activeView = activeScreens.Find(x => x.ViewModel == viewModel);
            return activeView != null ? CloseInternal(activeView, token) : UniTask.CompletedTask;
        }

        public bool IsOpen<TViewModel>(TViewModel viewModel)
            where TViewModel : ViewModel
        {
            return activeScreens.Any(screen => screen.ViewModel == viewModel);
        }

        private View<TViewModel> ResolveViewInternal<TViewModel>(out UILayer layer, out PopupOptions popupOptions,
            Action<TViewModel> beforeAttach = null)
            where TViewModel : ViewModel
        {
            var viewModel = screenResolver.Resolve<TViewModel>();
            beforeAttach?.Invoke(viewModel);

            var entry = screensCatalog.Get(UIId.OfScreen(typeof(TViewModel)));
            layer = entry.layer;
            popupOptions = entry.popupOptions.ToOptions();
            var container = uiRootProvider.GetLayerRoot(layer);

            return viewFactory.Create<View<TViewModel>, TViewModel>(viewModel, container);
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
            await activeView.View.HideAsync(token);
            activeView.View.Dispose();

            activeScreens.Remove(activeView);
            popupStack.Remove(activeView);

            var stackIndex = screenStack.IndexOf(activeView);
            if (stackIndex >= 0) {
                var wasTop = stackIndex == screenStack.Count - 1;
                screenStack.RemoveAt(stackIndex);

                if (wasTop && screenStack.Count > 0) {
                    await screenStack[^1].View.ShowAsync(token);
                }
            }

            SetTopInteractive(CurrentTop());
        }
    }
}

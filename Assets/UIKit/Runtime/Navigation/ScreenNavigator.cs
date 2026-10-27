using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    internal sealed class ScreenNavigator : IScreenNavigator
    {
        private readonly ScreenRegistry screenRegistry;
        private readonly ViewFactory viewFactory;
        private readonly IUIRootProvider rootProvider;
        private readonly NavigationGate gate;

        // Everything open or opening. The stacks below only hold entries once they are pushed.
        private readonly List<ActiveScreen> active = new();
        private readonly List<ActiveScreen> screens = new();
        private readonly List<ActiveScreen> popups = new();

        // Closed but still playing their hide animation. No longer open, yet a group or context
        // disposed meanwhile still has to dispose them right away rather than after the animation.
        private readonly List<ActiveScreen> closing = new();

        private VisualElement screenLayer;
        private VisualElement popupLayer;
        private ActiveScreen focusedTop;
        private bool disposed;

        public ScreenNavigator(ScreenRegistry screenRegistry, ViewFactory viewFactory,
            IUIRootProvider rootProvider, NavigationGate gate)
        {
            this.screenRegistry = screenRegistry;
            this.viewFactory = viewFactory;
            this.rootProvider = rootProvider;
            this.gate = gate;
        }

        public ViewModel Top => CurrentTop()?.ViewModel;

        public Awaitable<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default)
            where TViewModel : ViewModel, IResultViewModel<TResult>
        {
            return RunCoreAsync<TViewModel, TResult>(null, token);
        }

        public Awaitable<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IResultViewModel<TResult>, IPayloadReceiver<TPayload>
        {
            return RunCoreAsync<TViewModel, TResult>(viewModel => viewModel.SetPayload(payload), token);
        }

        public Awaitable<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            return OpenCoreAsync<TViewModel>(null, false, token);
        }

        public Awaitable<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            return OpenCoreAsync<TViewModel>(viewModel => viewModel.SetPayload(payload), false, token);
        }

        public Awaitable<ScreenHandle<TViewModel>> ReplaceAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            return OpenCoreAsync<TViewModel>(null, true, token);
        }

        public Awaitable<ScreenHandle<TViewModel>> ReplaceAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            return OpenCoreAsync<TViewModel>(viewModel => viewModel.SetPayload(payload), true, token);
        }

        public async Awaitable CloseAsync(ViewModel viewModel, CancellationToken token = default)
        {
            if (viewModel == null) {
                throw new ArgumentNullException(nameof(viewModel));
            }

            if (disposed) {
                return;
            }

            await gate.WaitAsync($"close {viewModel.GetType().Name}");

            try {
                // Looked up under the gate so another operation cannot close it between finding
                // it here and starting its transition.
                var entry = Find(viewModel);

                if (entry != null) {
                    await CloseInternalAsync(entry, token);
                }
            } finally {
                await EndOperationAsync();
            }
        }

        public async Awaitable<bool> GoBackAsync(CancellationToken token = default)
        {
            if (disposed) {
                return false;
            }

            await gate.WaitAsync("go back");

            try {
                var target = popups.Count > 0 ? popups[^1]
                    : screens.Count > 1 ? screens[^1]
                    : null;

                if (target == null) {
                    return false;
                }

                await CloseInternalAsync(target, token);
                return true;
            } finally {
                await EndOperationAsync();
            }
        }

        public async Awaitable PopToRootAsync(CancellationToken token = default)
        {
            if (disposed) {
                return;
            }

            await gate.WaitAsync("pop to root");

            try {
                await CloseStackedAsync(1, token);
            } finally {
                await EndOperationAsync();
            }
        }

        public async Awaitable CloseAllAsync(CancellationToken token = default)
        {
            if (disposed) {
                return;
            }

            await gate.WaitAsync("close all");

            try {
                await CloseStackedAsync(0, token);
            } finally {
                await EndOperationAsync();
            }
        }

        public bool IsOpen(ViewModel viewModel) => viewModel != null && Find(viewModel) != null;

        /// <summary>
        /// Force-closes every screen registered by <paramref name="owner"/>, without hide
        /// animations. Called as that group is disposed.
        /// </summary>
        /// <remarks>
        /// Synchronous and ungated, unlike every other close: the group's services are going
        /// away, so a hide animation would run view code against a half torn-down scope, and
        /// waiting for the gate could strand the UI behind a transition that cannot finish. An
        /// open still in flight is marked dead and aborts at its next step.
        /// </remarks>
        public void DisposeScreensOwnedBy(ScreenGroup owner)
        {
            if (disposed) {
                return;
            }

            var removedAny = false;

            for (var i = active.Count - 1; i >= 0; i--) {
                var entry = active[i];

                if (!ReferenceEquals(entry.Owner, owner)) {
                    continue;
                }

                active.RemoveAt(i);
                ForceDispose(entry);
                removedAny = true;
            }

            for (var i = closing.Count - 1; i >= 0; i--) {
                if (ReferenceEquals(closing[i].Owner, owner)) {
                    ForceDispose(closing[i]);
                }
            }

            if (!removedAny) {
                return;
            }

            UpdateInteractivity();

            // Showing the uncovered screen plays its show animation, so it queues behind any
            // operation that is running rather than racing it.
            RevealQueuedAsync().Forget();
        }

        // Called by UIKitContext.Dispose: force-closes everything, like DisposeScreensOwnedBy.
        public void DisposeAll()
        {
            if (disposed) {
                return;
            }

            disposed = true;
            gate.StopWatching();

            for (var i = active.Count - 1; i >= 0; i--) {
                ForceDispose(active[i]);
            }

            for (var i = closing.Count - 1; i >= 0; i--) {
                ForceDispose(closing[i]);
            }

            active.Clear();
            screens.Clear();
            popups.Clear();
            focusedTop = null;

            if (screenLayer != null) {
                screenLayer.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
                popupLayer.UnregisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
                popupLayer.UnregisterCallback<PointerDownEvent>(OnPopupLayerPointerDown, TrickleDown.TrickleDown);
                screenLayer.UnregisterCallback<AttachToPanelEvent>(OnLayerAttached);
                popupLayer.UnregisterCallback<AttachToPanelEvent>(OnLayerAttached);
                popupLayer.pickingMode = PickingMode.Ignore;
            }
        }

        private async Awaitable<TResult> RunCoreAsync<TViewModel, TResult>(Action<TViewModel> beforeAttach,
            CancellationToken token)
            where TViewModel : ViewModel, IResultViewModel<TResult>
        {
            var entry = await OpenEntryAsync(beforeAttach, false, token);

            try {
                // Outside the gate: this waits on the user, and holding the gate here would
                // stall every other navigation until the screen closes.
                return await ((TViewModel)entry.ViewModel).WaitForResultAsync(token);
            } finally {
                // CancellationToken.None so a cancelled wait still plays the hide animation
                // rather than snapping the view away.
                await CloseEntryAsync(entry, CancellationToken.None);
            }
        }

        private async Awaitable<ScreenHandle<TViewModel>> OpenCoreAsync<TViewModel>(Action<TViewModel> beforeAttach,
            bool replace, CancellationToken token)
            where TViewModel : ViewModel
        {
            var entry = await OpenEntryAsync(beforeAttach, replace, token);
            return new ScreenHandle<TViewModel>(this, (TViewModel)entry.ViewModel);
        }

        private async Awaitable<ActiveScreen> OpenEntryAsync<TViewModel>(Action<TViewModel> beforeAttach,
            bool replace, CancellationToken token)
            where TViewModel : ViewModel
        {
            ThrowIfDisposed();

            await gate.WaitAsync($"{(replace ? "replace with" : "open")} {typeof(TViewModel).Name}");

            try {
                ThrowIfDisposed();
                token.ThrowIfCancellationRequested();

                var replaced = replace ? TopOf(ScreenInfo.Of(typeof(TViewModel)).Layer) : null;

                var entry = Resolve(beforeAttach);
                await PushAsync(entry, token);

                if (replaced != null) {
                    // The open has already succeeded, so the replaced entry closes regardless of
                    // the token.
                    await CloseInternalAsync(replaced, CancellationToken.None);
                }

                return entry;
            } finally {
                await EndOperationAsync();
            }
        }

        private async Awaitable CloseEntryAsync(ActiveScreen entry, CancellationToken token)
        {
            if (disposed) {
                return;
            }

            await gate.WaitAsync($"close {entry.ViewModel.GetType().Name}");

            try {
                await CloseInternalAsync(entry, token);
            } finally {
                await EndOperationAsync();
            }
        }

        private async Awaitable RevealQueuedAsync()
        {
            await gate.WaitAsync("show uncovered screen");
            await EndOperationAsync();
        }

        private ActiveScreen Resolve<TViewModel>(Action<TViewModel> beforeAttach) where TViewModel : ViewModel
        {
            var binding = screenRegistry.Get(typeof(TViewModel));

            if (binding.CreateViewModel == null) {
                throw new InvalidOperationException(
                    $"'{typeof(TViewModel).Name}' is registered as a child view only (RegisterView), " +
                    "so it cannot be opened as a screen. Register it with Register/RegisterScreen instead.");
            }

            var info = ScreenInfo.Of(typeof(TViewModel));
            var created = binding.CreateViewModel();

            if (created is not TViewModel viewModel) {
                created?.Dispose();
                throw new InvalidOperationException(
                    $"The ViewModel factory for '{typeof(TViewModel).Name}' returned " +
                    $"{(created == null ? "null" : $"a {created.GetType().Name}")}.");
            }

            try {
                beforeAttach?.Invoke(viewModel);
                EnsureLayers();

                var view = viewFactory.CreateView(typeof(TViewModel), viewModel, LayerRoot(info.Layer), true);

                // Focusable so the top screen can hold focus, which is what routes Escape /
                // gamepad cancel to it even when none of its controls is focused.
                view.Root.focusable = true;

                var entry = new ActiveScreen(viewModel, view, binding.Owner, info);

                if (info.Dismiss.HasFlag(DismissTriggers.NavigationCancel)) {
                    view.Root.RegisterCallback<NavigationCancelEvent, ActiveScreen>(OnNavigationCancel, entry);
                }

                return entry;
            } catch {
                // The view disposes the ViewModel it took, but a failure before that - in the
                // payload, a missing UXML, the view factory - would otherwise leak it.
                viewModel.Dispose();
                throw;
            }
        }

        private async Awaitable PushAsync(ActiveScreen entry, CancellationToken token)
        {
            active.Add(entry);

            try {
                if (entry.Info.Layer == UILayer.Screen && screens.Count > 0) {
                    var covered = screens[^1];

                    if (!covered.Hidden) {
                        covered.Hidden = true;
                        await HideAsync(covered, token);
                        ThrowIfDead(entry);
                    }
                }

                StackOf(entry.Info.Layer).Add(entry);

                await ShowAsync(entry, token);
                ThrowIfDead(entry);
            } catch {
                // A failed or cancelled open must not leave the view attached and untracked. A
                // screen it had already covered is shown again by EndOperationAsync.
                Untrack(entry);
                entry.View.Dispose();

                // A cancelled animation of a disposed group surfaces as the documented "group
                // disposed" cancellation rather than a bare one.
                ThrowIfDead(entry);
                throw;
            }

            try {
                entry.ViewModel.OnOpened();
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }

        private async Awaitable CloseInternalAsync(ActiveScreen entry, CancellationToken token)
        {
            // Untracked synchronously: a second close for the same entry can arrive while the hide
            // animation below is running, and would otherwise dispose it twice.
            if (!active.Remove(entry)) {
                screens.Remove(entry);
                popups.Remove(entry);
                return;
            }

            try {
                // Closed from outside: resolve it as cancelled, so a RunAsync waiting on it returns
                // instead of throwing.
                if (entry.ViewModel is ICancelable cancelable && entry.ViewModel is not IResultState { HasResult: true }) {
                    cancelable.Cancel();
                }

                entry.ViewModel.OnClosed();
            } catch (Exception exception) {
                Debug.LogException(exception);
            }

            closing.Add(entry);

            try {
                if (!entry.Hidden) {
                    entry.Hidden = true;
                    await HideAsync(entry, token);
                }
            } catch (OperationCanceledException) when (entry.IsDead) {
                // Its group was disposed mid-animation, which already disposed it: the close
                // the caller asked for has happened.
            } finally {
                // Cleanup, so it runs even when the hide animation was cancelled - otherwise the
                // view would stay attached and the screen underneath hidden forever.
                closing.Remove(entry);
                screens.Remove(entry);
                popups.Remove(entry);
                entry.View.Dispose();
            }
        }

        private async Awaitable CloseStackedAsync(int screensToKeep, CancellationToken token)
        {
            while (popups.Count > 0) {
                await CloseInternalAsync(popups[^1], token);
            }

            // Only the top screen is visible, so only it plays a hide animation; the uncovered
            // bottom screen is shown once, by EndOperationAsync.
            while (screens.Count > screensToKeep) {
                await CloseInternalAsync(screens[^1], token);
            }
        }

        // Ends every gated operation: shows the top screen if it was left hidden - its cover was
        // closed, or an open failed after hiding it - updates focus and passes the gate on.
        private async Awaitable EndOperationAsync()
        {
            try {
                if (!disposed) {
                    var top = screens.Count > 0 ? screens[^1] : null;

                    if (top is { Hidden: true }) {
                        top.Hidden = false;
                        await ShowAsync(top, CancellationToken.None);
                    }

                    UpdateInteractivity();
                }
            } catch (Exception exception) {
                Debug.LogException(exception);
            } finally {
                gate.Release();
            }
        }

        // Show and hide animations also stop when the entry is force-disposed, so an animation
        // still running never touches a view whose root is already gone.
        private static async Awaitable ShowAsync(ActiveScreen entry, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Lifetime);
            await entry.View.ShowAsync(linked.Token);
        }

        private static async Awaitable HideAsync(ActiveScreen entry, CancellationToken token)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, entry.Lifetime);
            await entry.View.HideAsync(linked.Token);
        }

        private void ForceDispose(ActiveScreen entry)
        {
            entry.Kill();
            screens.Remove(entry);
            popups.Remove(entry);

            try {
                entry.View.Dispose();
            } catch (Exception exception) {
                Debug.LogException(exception);
            }
        }

        private void Untrack(ActiveScreen entry)
        {
            active.Remove(entry);
            screens.Remove(entry);
            popups.Remove(entry);
        }

        // Only the top screen or popup takes input. Popups block pointer input to everything
        // below them through the popup layer, and focus is kept on the top entry; nothing is
        // disabled, so covered views keep their normal look.
        private void UpdateInteractivity()
        {
            if (popupLayer != null) {
                popupLayer.pickingMode = popups.Count > 0 ? PickingMode.Position : PickingMode.Ignore;
            }

            var top = CurrentTop();

            if (top == focusedTop) {
                return;
            }

            // Focus() does nothing on an element outside a panel - the layers may reach theirs a
            // frame later, e.g. with a PanelRenderer. Left unrecorded, OnLayerAttached retries.
            if (top != null && top.View.Root?.panel == null) {
                return;
            }

            focusedTop = top;
            top?.View.Root?.Focus();
        }

        // A layer reaching a panel, first or again after its host rebuilt the root, is when the
        // top entry can take focus.
        private void OnLayerAttached(AttachToPanelEvent evt)
        {
            if (disposed) {
                return;
            }

            focusedTop = null;
            UpdateInteractivity();
        }

        private void EnsureLayers()
        {
            if (screenLayer != null) {
                return;
            }

            screenLayer = rootProvider.GetLayerRoot(UILayer.Screen);
            popupLayer = rootProvider.GetLayerRoot(UILayer.Popup);

            screenLayer.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            popupLayer.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            popupLayer.RegisterCallback<PointerDownEvent>(OnPopupLayerPointerDown, TrickleDown.TrickleDown);
            screenLayer.RegisterCallback<AttachToPanelEvent>(OnLayerAttached);
            popupLayer.RegisterCallback<AttachToPanelEvent>(OnLayerAttached);
        }

        // Keyboard and gamepad navigation can still reach a covered view: move focus back.
        private void OnFocusIn(FocusInEvent evt)
        {
            var top = CurrentTop();
            var root = top?.View.Root;

            if (root == null || evt.target is not VisualElement target || target == root || root.Contains(target)) {
                return;
            }

            // Deferred: changing focus while a focus change is being dispatched is overridden by it.
            root.schedule.Execute(() => {
                if (CurrentTop() == top) {
                    top.View.Root?.Focus();
                }
            });
        }

        private void OnPopupLayerPointerDown(PointerDownEvent evt)
        {
            var top = popups.Count > 0 ? popups[^1] : null;
            var root = top?.View.Root;

            if (root == null) {
                return;
            }

            var target = evt.target as VisualElement;

            if (target != null && target != root && root.Contains(target)) {
                return;
            }

            // Outside the top popup: a lower popup must not react to it.
            if (target != root) {
                evt.StopImmediatePropagation();
            }

            if (top.Info.Dismiss.HasFlag(DismissTriggers.BackdropClick)) {
                CloseAsync(top.ViewModel).Forget();
            }
        }

        private void OnNavigationCancel(NavigationCancelEvent evt, ActiveScreen entry)
        {
            if (entry != CurrentTop()) {
                return;
            }

            if (entry.Info.Layer == UILayer.Screen && screens.Count <= 1) {
                return;
            }

            evt.StopPropagation();
            CloseAsync(entry.ViewModel).Forget();
        }

        private ActiveScreen Find(ViewModel viewModel)
        {
            foreach (var entry in active) {
                if (ReferenceEquals(entry.ViewModel, viewModel)) {
                    return entry;
                }
            }

            return null;
        }

        private ActiveScreen CurrentTop() =>
            popups.Count > 0 ? popups[^1]
            : screens.Count > 0 ? screens[^1]
            : null;

        private ActiveScreen TopOf(UILayer layer)
        {
            var stack = StackOf(layer);
            return stack.Count > 0 ? stack[^1] : null;
        }

        private List<ActiveScreen> StackOf(UILayer layer) => layer == UILayer.Popup ? popups : screens;

        private VisualElement LayerRoot(UILayer layer) => layer == UILayer.Popup ? popupLayer : screenLayer;

        private static void ThrowIfDead(ActiveScreen entry)
        {
            if (entry.IsDead) {
                throw new OperationCanceledException(
                    $"'{entry.ViewModel.GetType().Name}' was closed while opening, because the " +
                    $"{nameof(ScreenGroup)} it was registered in was disposed.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed) {
                throw new ObjectDisposedException(nameof(UIKitContext));
            }
        }
    }
}

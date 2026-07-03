using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UIToolkitMVVM.Views;

namespace UIToolkitMVVM.Navigation
{
    public class UIService : IUIService, IDisposable
    {
        private sealed class ActiveScreen
        {
            public IView View;
            public VisualElement Root;
        }

        private readonly IUIScreenRegistry uiScreenRegistry;
        private readonly IUIScreenFactory uiScreenFactory;
        private readonly RootPanelProvider rootPanelProvider;

        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly List<ActiveScreen> activePopups = new();
        private ActiveScreen currentScreen;

        public UIService(IUIScreenFactory uiScreenFactory, IUIScreenRegistry uiScreenRegistry,
            RootPanelProvider rootPanelProvider)
        {
            this.uiScreenRegistry = uiScreenRegistry;
            this.uiScreenFactory = uiScreenFactory;
            this.rootPanelProvider = rootPanelProvider;
        }

        public async UniTask<TScreen> ShowScreenAsync<TScreen>(CancellationToken token = default)
            where TScreen : IScreen
        {
            await gate.WaitAsync(token);
            try {
                await HideAllPopupsInternalAsync(token);

                if (currentScreen != null) {
                    var previousScreen = currentScreen;
                    currentScreen = null;
                    await HideInternalAsync(previousScreen, token);
                }

                var screen = await CreateAndShowAsync<TScreen>(token);
                currentScreen = screen;

                return (TScreen)screen.View;
            }
            finally {
                gate.Release();
            }
        }

        public async UniTask<TResult> ShowScreenAndWaitResultAsync<TScreen, TResult>(CancellationToken token = default)
            where TScreen : IScreen, IResultView<TResult>
        {
            var view = await ShowScreenAsync<TScreen>(token);

            try {
                return await view.WaitForResultAsync(token);
            }
            finally {
                await HideScreenAsync(view, CancellationToken.None);
            }
        }

        public async UniTask HideScreenAsync(IScreen screen, CancellationToken token = default)
        {
            if (screen == null) {
                return;
            }

            await gate.WaitAsync(token);
            try {
                if (currentScreen == null || !ReferenceEquals(currentScreen.View, screen)) {
                    return;
                }

                var screenToHide = currentScreen;
                currentScreen = null;
                await HideInternalAsync(screenToHide, token);
            }
            finally {
                gate.Release();
            }
        }

        public async UniTask<TPopup> ShowPopupAsync<TPopup>(CancellationToken token = default) where TPopup : IPopup
        {
            await gate.WaitAsync(token);
            try {
                var popup = await CreateAndShowAsync<TPopup>(token);
                activePopups.Add(popup);

                return (TPopup)popup.View;
            }
            finally {
                gate.Release();
            }
        }

        public async UniTask<TResult> ShowPopupAndWaitResultAsync<TPopup, TResult>(CancellationToken token = default)
            where TPopup : IPopup, IResultView<TResult>
        {
            var view = await ShowPopupAsync<TPopup>(token);

            try {
                return await view.WaitForResultAsync(token);
            }
            finally {
                await HidePopupAsync(view, CancellationToken.None);
            }
        }

        public async UniTask HidePopupAsync(IPopup popup, CancellationToken token = default)
        {
            if (popup == null) {
                return;
            }

            await gate.WaitAsync(token);
            try {
                var index = activePopups.FindIndex(p => ReferenceEquals(p.View, popup));
                if (index < 0) {
                    return;
                }

                var popupToHide = activePopups[index];
                activePopups.RemoveAt(index);
                await HideInternalAsync(popupToHide, token);
            }
            finally {
                gate.Release();
            }
        }

        public async UniTask ReleaseAllAsync(CancellationToken token = default)
        {
            await gate.WaitAsync(token);
            try {
                var screens = new List<ActiveScreen>(activePopups);
                activePopups.Clear();

                if (currentScreen != null) {
                    screens.Add(currentScreen);
                    currentScreen = null;
                }

                await UniTask.WhenAll(screens.Select(screen => HideInternalAsync(screen, token)));
            }
            finally {
                gate.Release();
            }
        }

        public void Dispose() => ReleaseAllAsync().Forget();

        private async UniTask HideAllPopupsInternalAsync(CancellationToken token)
        {
            if (activePopups.Count == 0) {
                return;
            }

            var popups = new List<ActiveScreen>(activePopups);
            activePopups.Clear();

            await UniTask.WhenAll(popups.Select(popup => HideInternalAsync(popup, token)));
        }

        private async UniTask<ActiveScreen> CreateAndShowAsync<TView>(CancellationToken token) where TView : IView
        {
            var view = uiScreenFactory.Create<TView>();
            var screenId = view.ViewId;
            var entry = uiScreenRegistry.Get(screenId);

            var container = new VisualElement { name = $"{screenId}-root" };
            entry.CloneTree(container);
            container.StretchToParentSize();
            rootPanelProvider.Root.Add(container);

            view.Bind(container);

            try {
                await view.ShowAsync(token);
            }
            catch {
                view.Dispose();
                container.RemoveFromHierarchy();
                throw;
            }

            return new ActiveScreen { View = view, Root = container };
        }

        private static async UniTask HideInternalAsync(ActiveScreen screen, CancellationToken token)
        {
            try {
                await screen.View.HideAsync(token);
            }
            finally {
                screen.View.Dispose();
                screen.Root.RemoveFromHierarchy();
            }
        }
    }
}

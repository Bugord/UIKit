using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;

namespace UIKit.Runtime.Core
{
    public class ScreenNavigator : IScreenNavigator
    {
        private readonly IScreenResolver screenResolver;
        private readonly IViewFactory viewFactory;
        private readonly IUIRootProvider uiRootProvider;
        private readonly ScreenCatalog screensCatalog;

        private readonly List<ActiveScreen> activeScreens = new();
        private readonly List<ActiveScreen> screenStack = new();

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
            var view = ResolveViewInternal<TViewModel>(out var layer);
            var activeScreen = new ActiveScreen(view.ViewModel, view);

            await PushAsync(activeScreen, layer, token);
            var result = await view.ViewModel.WaitForResultAsync(token);
            await CloseInternal(activeScreen, token);

            return result;
        }

        public async UniTask<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, vm => vm.SetPayload(payload));
            var activeScreen = new ActiveScreen(view.ViewModel, view);

            await PushAsync(activeScreen, layer, token);
            var result = await view.ViewModel.WaitForResultAsync(token);
            await CloseInternal(activeScreen, token);

            return result;
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var view = ResolveViewInternal<TViewModel>(out var layer);
            var activeScreen = new ActiveScreen(view.ViewModel, view);

            await PushAsync(activeScreen, layer, token);

            return new ScreenHandle<TViewModel>(this, view.ViewModel);
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(out var layer, vm => vm.SetPayload(payload));
            var activeScreen = new ActiveScreen(view.ViewModel, view);

            await PushAsync(activeScreen, layer, token);

            return new ScreenHandle<TViewModel>(this, view.ViewModel);
        }

        public UniTask CloseAsync<TViewModel>(TViewModel viewModel, CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var activeScreen = activeScreens.Find(x => x.ViewModel == viewModel);
            return activeScreen != null ? CloseInternal(activeScreen, token) : UniTask.CompletedTask;
        }

        public bool IsOpen<TViewModel>(TViewModel viewModel)
            where TViewModel : ViewModel
        {
            return activeScreens.Any(screen => screen.ViewModel == viewModel);
        }

        private View<TViewModel> ResolveViewInternal<TViewModel>(out UILayer layer,
            Action<TViewModel> beforeAttach = null)
            where TViewModel : ViewModel
        {
            var viewModel = screenResolver.Resolve<TViewModel>();
            beforeAttach?.Invoke(viewModel);

            layer = screensCatalog.Get(UIId.OfScreen(typeof(TViewModel))).layer;
            var container = uiRootProvider.GetLayerRoot(layer);

            return viewFactory.Create<View<TViewModel>, TViewModel>(viewModel, container);
        }

        private async UniTask PushAsync(ActiveScreen activeScreen, UILayer layer, CancellationToken token)
        {
            activeScreens.Add(activeScreen);

            if (layer == UILayer.Screen) {
                if (screenStack.Count > 0) {
                    await screenStack[^1].View.HideAsync(token);
                }

                screenStack.Add(activeScreen);
            }

            await activeScreen.View.ShowAsync(token);
        }

        private async UniTask CloseInternal(ActiveScreen activeScreen, CancellationToken token)
        {
            await activeScreen.View.HideAsync(token);
            activeScreen.View.Dispose();

            activeScreens.Remove(activeScreen);

            var stackIndex = screenStack.IndexOf(activeScreen);
            if (stackIndex < 0) {
                return;
            }

            var wasTop = stackIndex == screenStack.Count - 1;
            screenStack.RemoveAt(stackIndex);

            if (wasTop && screenStack.Count > 0) {
                await screenStack[^1].View.ShowAsync(token);
            }
        }
    }
}
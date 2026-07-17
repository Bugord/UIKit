using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UIKit.Runtime.Utils;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public class ScreenNavigator : IScreenNavigator
    {
        private readonly IScreenResolver screenResolver;
        private readonly IUIRootProvider uiRootProvider;
        private readonly ScreenCatalog screensCatalog;

        private readonly List<ActiveScreen> activeScreens = new();

        public ScreenNavigator(IScreenResolver screenResolver, IUIRootProvider uiRootProvider,
            ScreenCatalog screensCatalog)
        {
            this.screenResolver = screenResolver;
            this.screensCatalog = screensCatalog;
            this.uiRootProvider = uiRootProvider;
        }

        public async UniTask<TResult> RunAsync<TViewModel, TResult>(CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>
        {
            var view = ResolveViewInternal<TViewModel>();
            var activeScreen = new ActiveScreen(view.ViewModel, view);
            activeScreens.Add(activeScreen);

            await view.ShowAsync(token);
            var result = await view.ViewModel.WaitForResultAsync(token);
            await CloseInternal(activeScreen, token);

            return result;
        }

        public async UniTask<TResult> RunAsync<TViewModel, TPayload, TResult>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ResultViewModel<TResult>, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(vm => vm.SetPayload(payload));

            var activeScreen = new ActiveScreen(view.ViewModel, view);
            activeScreens.Add(activeScreen);

            await view.ShowAsync(token);
            var result = await view.ViewModel.WaitForResultAsync(token);
            await CloseInternal(activeScreen, token);

            return result;
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel>(CancellationToken token = default)
            where TViewModel : ViewModel
        {
            var view = ResolveViewInternal<TViewModel>();
            activeScreens.Add(new ActiveScreen(view.ViewModel, view));
            await view.ShowAsync(token);

            return new ScreenHandle<TViewModel>(this, view.ViewModel);
        }

        public async UniTask<ScreenHandle<TViewModel>> OpenAsync<TViewModel, TPayload>(TPayload payload,
            CancellationToken token = default)
            where TViewModel : ViewModel, IPayloadReceiver<TPayload>
        {
            var view = ResolveViewInternal<TViewModel>(vm => vm.SetPayload(payload));

            activeScreens.Add(new ActiveScreen(view.ViewModel, view));
            await view.ShowAsync(token);

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

        private View<TViewModel> ResolveViewInternal<TViewModel>(Action<TViewModel> beforeAttach = null)
            where TViewModel : ViewModel
        {
            var viewModel = screenResolver.Resolve<TViewModel>();
            beforeAttach?.Invoke(viewModel);

            var view = screenResolver.ResolveView<TViewModel>();
            view.Bind(viewModel);

            var viewModelId = UIId.OfScreen(typeof(TViewModel));
            var root = new VisualElement { name = $"{viewModelId}Root" };
            root.StretchToParent();

            var entry = screensCatalog.Get(viewModelId);
            entry.uxml.CloneTree(root);
            view.Attach(root);

            var container = uiRootProvider.GetLayerRoot(entry.layer);
            container.Add(view.Root);

            return view;
        }

        private async UniTask CloseInternal(ActiveScreen activeScreen, CancellationToken token)
        {
            await activeScreen.View.HideAsync(token);
            activeScreen.View.Dispose();

            activeScreens.Remove(activeScreen);
        }
    }
}
using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public abstract class View<TViewModel> : IView, IViewBinder where TViewModel : ViewModel
    {
        public VisualElement Root { get; private set; }
        public TViewModel ViewModel { get; private set; }
        protected IViewFactory ViewFactory { get; private set; }

        private bool disposeViewModel = true;

        public void Bind(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public void Attach(VisualElement root)
        {
            Root = root;
            OnAttach();
        }

        internal void SetViewFactory(IViewFactory viewFactory)
        {
            ViewFactory = viewFactory;
        }

        void IViewBinder.SetViewFactory(IViewFactory viewFactory) => SetViewFactory(viewFactory);

        void IViewBinder.BindModel(ViewModel viewModel, bool disposeViewModel)
        {
            Bind((TViewModel)viewModel);
            this.disposeViewModel = disposeViewModel;
        }

        public virtual UniTask ShowAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Root.style.display = DisplayStyle.Flex;
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Root.style.display = DisplayStyle.None;
            return UniTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            Root?.RemoveFromHierarchy();
            Root = null;

            if (disposeViewModel) {
                ViewModel?.Dispose();
            }
        }

        protected virtual void OnAttach()
        {
        }
    }
}

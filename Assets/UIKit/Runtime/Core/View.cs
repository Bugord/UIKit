using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public abstract class View<TViewModel> : IView where TViewModel : ViewModel
    {
        public VisualElement Root { get; private set; }
        public TViewModel ViewModel { get; private set; }
        protected IViewFactory ViewFactory { get; private set; }

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

        public virtual UniTask ShowAsync(CancellationToken token)
        {
            Root.style.display = DisplayStyle.Flex;
            return UniTask.CompletedTask;
        }

        public virtual UniTask HideAsync(CancellationToken token)
        {
            Root.style.display = DisplayStyle.None;
            return UniTask.CompletedTask;
        }

        public virtual void Dispose()
        {
            Root?.RemoveFromHierarchy();
            Root = null;
            ViewModel.Dispose();
        }

        protected virtual void OnAttach()
        {
        }
    }
}

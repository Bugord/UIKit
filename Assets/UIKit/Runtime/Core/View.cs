using System.Threading;
using Cysharp.Threading.Tasks;
using UIKit.Runtime.Abstractions;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public abstract class View<TViewModel> : IView where TViewModel : ViewModel
    {
        public VisualElement Root { get; private set; }
        public TViewModel ViewModel { get; }

        protected View(TViewModel viewModel)
        {
            ViewModel = viewModel;
        }

        public void Attach(VisualElement root)
        {
            Root = root;
            OnAttach();
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
            Root = null;
            ViewModel.Dispose();
        }

        protected virtual void OnAttach()
        {
        }
    }
}

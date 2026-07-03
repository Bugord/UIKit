using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;
using UIToolkitMVVM.ViewModels;

namespace UIToolkitMVVM.Views
{
    public abstract class View<TModel> : IView where TModel : ViewModel
    {
        public abstract string ViewId { get; }
        public VisualElement Root { get; private set; }
        protected TModel Model { get; }

        protected View(TModel model)
        {
            Model = model;
        }

        public void Bind(VisualElement root)
        {
            Root = root;
            OnBind();
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
            Model.Dispose();
        }

        protected virtual void OnBind()
        {
        }
    }
}

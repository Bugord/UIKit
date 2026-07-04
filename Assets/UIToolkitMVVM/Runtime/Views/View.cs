using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;
using UIToolkitMVVM.Utils;
using UIToolkitMVVM.ViewModels;

namespace UIToolkitMVVM.Views
{
    public abstract class View<TModel> : IView where TModel : ViewModel
    {
        private static readonly Dictionary<Type, string> IdCache = new();

        public virtual string ViewId
        {
            get
            {
                var type = GetType();
                if (!IdCache.TryGetValue(type, out var id)) {
                    id = UIIdConventions.ResolveScreenId(type);
                    IdCache[type] = id;
                }
                return id;
            }
        }

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

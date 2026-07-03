using UI.Navigation;
using UnityEngine.UIElements;

namespace UI.Widgets
{
    public abstract class Widget<TModel> : IWidget where TModel : ViewModel
    {
        public VisualElement Root { get; private set; }
        public TModel Model { get; }

        protected Widget(TModel model)
        {
            Model = model;
        }

        public void Bind(VisualElement root)
        {
            Root = root;
            OnBind();
        }

        protected virtual void OnBind()
        {
        }

        public virtual void Dispose()
        {
            Model.Dispose();
            Root = null;
        }
    }
}
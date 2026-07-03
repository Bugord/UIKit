using UIToolkitMVVM.ViewModels;

namespace UIToolkitMVVM.Widgets
{
    public abstract class LoadableWidget<TViewModel> : Widget<TViewModel>, ILoadableWidget where TViewModel : ViewModel
    {
        protected LoadableWidget(TViewModel model) : base(model)
        {
        }

        public abstract string WidgetId { get; }
    }
}
using UI.Navigation;

namespace UI.Widgets
{
    public abstract class LoadableWidget<TViewModel> : Widget<TViewModel>, ILoadableWidget where TViewModel : ViewModel
    {
        protected LoadableWidget(TViewModel model) : base(model)
        {
        }

        public abstract string WidgetId { get; }
    }
}
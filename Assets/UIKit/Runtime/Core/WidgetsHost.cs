using System;
using System.Collections.Generic;
using UIKit.Runtime.Core;
using UnityEngine.UIElements;
using VContainer;

namespace UIKit.Runtime.Widgets
{
    public sealed class WidgetHost : IDisposable
    {
        private readonly IWidgetFactory widgetFactory;
        private readonly WidgetCatalog catalog;
        private readonly List<IWidget> widgets = new();

        public WidgetHost(IWidgetFactory widgetFactory, WidgetCatalog catalog)
        {
            this.widgetFactory = widgetFactory;
            this.catalog = catalog;
        }

        public TWidget Attach<TWidget>(VisualElement anchor) where TWidget : IWidget
        {
            var widget = widgetFactory.Create<TWidget>();
            var widgetId = UIId.OfWidget(typeof(TWidget));

            var container = new VisualElement { name = $"{widgetId}Root" };
            catalog.Get(widgetId).CloneTree(container);
            anchor.Add(container);

            widget.Bind(container);
            widgets.Add(widget);
            return widget;
        }

        public TWidget BindExisting<TWidget>(VisualElement element) where TWidget : IWidget
        {
            var widget = widgetFactory.Create<TWidget>();
            widget.Bind(element);
            widgets.Add(widget);
            return widget;
        }

        public void Detach(IWidget widget)
        {
            if (!widgets.Remove(widget)) {
                return;
            }

            widget.Root?.RemoveFromHierarchy();
            widget.Dispose();
        }

        public void Dispose()
        {
            foreach (var widget in widgets) {
                widget.Root?.RemoveFromHierarchy();
                widget.Dispose();
            }

            widgets.Clear();
        }
    }
}
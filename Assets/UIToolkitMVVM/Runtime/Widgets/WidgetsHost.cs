using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI.Widgets
{
    public class WidgetsHost : IDisposable
    {
        private readonly IWidgetsFactory widgetsFactory;
        private readonly IWidgetTemplateRegistry templateRegistry;
        private readonly List<IWidget> widgets = new();

        public WidgetsHost(IWidgetsFactory widgetsFactory, IWidgetTemplateRegistry templateRegistry)
        {
            this.widgetsFactory = widgetsFactory;
            this.templateRegistry = templateRegistry;
        }
        
        public TWidget Attach<TWidget>(VisualElement anchor) where TWidget : ILoadableWidget
        {
            var widget = widgetsFactory.Create<TWidget>();
            var container = new VisualElement { name = $"{widget.WidgetId}-root" };
            templateRegistry.Get(widget.WidgetId).CloneTree(container);
            anchor.Add(container);
 
            widget.Bind(container);
            widgets.Add(widget);
            return widget;
        }

        public TWidget AttachExisting<TWidget>(VisualElement widgetElement) where TWidget : IWidget
        {
            Debug.Log($"Attaching widget inside {widgetElement}");
            var widget = widgetsFactory.Create<TWidget>();
            widget.Bind(widgetElement);
            widgets.Add(widget);
            return widget;
        }

        public void Dispose()
        {
            foreach (var widget in widgets) {
                widget.Dispose();
            }
        }
    }
}
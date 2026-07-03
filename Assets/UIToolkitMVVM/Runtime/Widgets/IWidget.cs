using System;
using UnityEngine.UIElements;

namespace UIToolkitMVVM.Widgets
{
    public interface IWidget : IDisposable
    {
        VisualElement Root { get; }
        void Bind(VisualElement root);
    }

    public interface ILoadableWidget : IWidget
    {
        string WidgetId { get; }
    }
}
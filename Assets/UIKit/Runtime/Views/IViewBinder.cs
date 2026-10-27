using System;
using UnityEngine.UIElements;

namespace UIKit
{
    // The factory's side of View<TViewModel>, implemented explicitly so none of it is public API.
    internal interface IViewBinder
    {
        event Action<IView> Disposed;

        void Bind(ViewModel viewModel, bool disposeViewModel, ViewFactory viewFactory);

        VisualElement CreateRoot();

        void Attach(VisualElement root);
    }
}

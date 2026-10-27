using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit
{
    public interface IView : IDisposable
    {
        VisualElement Root { get; }
        ViewModel ViewModel { get; }
        bool IsDisposed { get; }
        Awaitable ShowAsync(CancellationToken token);
        Awaitable HideAsync(CancellationToken token);
    }
}

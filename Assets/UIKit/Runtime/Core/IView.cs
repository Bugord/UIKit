using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IView : IDisposable
    {
        VisualElement Root { get; }
        void Attach(VisualElement root);
        UniTask HideAsync(CancellationToken token);
        UniTask ShowAsync(CancellationToken token);
    }
}

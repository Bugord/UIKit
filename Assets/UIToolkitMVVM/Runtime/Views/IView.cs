using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace UI.Navigation
{
    public interface IView : IDisposable
    {
        string ViewId { get; }
        VisualElement Root { get; }
        void Bind(VisualElement root);
        UniTask HideAsync(CancellationToken token);
        UniTask ShowAsync(CancellationToken token);
    }
}

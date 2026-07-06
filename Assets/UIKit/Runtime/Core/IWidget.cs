using System;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public interface IWidget : IDisposable
    {
        VisualElement Root { get; }
        void Bind(VisualElement root);
    }
}
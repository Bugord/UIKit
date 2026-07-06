using System;
using UIKit.Runtime.Core;

namespace UIKit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UIScreenAttribute : Attribute
    {
        public string Id { get; }
        public UILayer Layer { get; }
        public bool LayerSpecified { get; }
        public bool CancelOnBackdropClick { get; init; } = true;
        public bool CancelOnNavigationCancel { get; init; } = true;
 
        public UIScreenAttribute(string id = null)
        {
            Id = id;
        }
 
        public UIScreenAttribute(UILayer layer, string id = null)
        {
            Layer = layer;
            LayerSpecified = true;
            Id = id;
        }
    }
}

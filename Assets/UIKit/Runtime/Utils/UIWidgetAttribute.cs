using System;

namespace UIKit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UIWidgetAttribute : Attribute
    {
        public string Id { get; }
        public bool Templated { get; }
 
        public UIWidgetAttribute(bool templated = false, string id = null)
        {
            Templated = templated;
            Id = id;
        }
    }
}

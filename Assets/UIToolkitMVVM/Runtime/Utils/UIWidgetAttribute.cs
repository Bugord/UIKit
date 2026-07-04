using System;

namespace UIToolkitMVVM.Utils
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UIWidgetAttribute : Attribute
    {
        public string Id { get; }

        public UIWidgetAttribute(string id = null)
        {
            Id = id;
        }
    }
}

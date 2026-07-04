using System;

namespace UIToolkitMVVM.Utils
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UIScreenAttribute : Attribute
    {
        public string Id { get; }

        public UIScreenAttribute(string id = null)
        {
            Id = id;
        }
    }
}

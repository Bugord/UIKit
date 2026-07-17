using System;

namespace UIKit.Runtime.Utils
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UIScreenAttribute : Attribute
    {
        public string Id { get; }

        public UIScreenAttribute(string id = null)
        {
            Id = id;
        }
    }
}

using System;

namespace UIKit.Runtime.Utils
{
    public static class UIIdConventions
    {
        public static string ResolveScreenId(Type type)
        {
            var attribute = (UIScreenAttribute)Attribute.GetCustomAttribute(type, typeof(UIScreenAttribute));
            return attribute?.Id ?? StripSuffix(type.Name, "View");
        }

        public static string ResolveWidgetId(Type type)
        {
            var attribute = (UIWidgetAttribute)Attribute.GetCustomAttribute(type, typeof(UIWidgetAttribute));
            return attribute?.Id ?? StripSuffix(type.Name, "Widget");
        }

        private static string StripSuffix(string name, string suffix)
        {
            return name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - suffix.Length)
                : name;
        }
    }
}

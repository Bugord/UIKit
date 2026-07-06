using System;
using System.Collections.Concurrent;
using System.Reflection;
using UIKit.Runtime.Utils;

namespace UIKit.Runtime.Core
{
    public static class UIId
    {
        private static readonly ConcurrentDictionary<Type, string> ScreenIds = new();
        private static readonly ConcurrentDictionary<Type, string> WidgetIds = new();

        public static string OfScreen(Type viewModelType) =>
            ScreenIds.GetOrAdd(viewModelType, static type =>
                type.GetCustomAttribute<UIScreenAttribute>()?.Id
                ?? Strip(type.Name, "ViewModel"));

        public static string OfWidget(Type widgetType) =>
            WidgetIds.GetOrAdd(widgetType, static type =>
                type.GetCustomAttribute<UIWidgetAttribute>()?.Id
                ?? Strip(type.Name, "Widget"));

        private static string Strip(string name, string suffix)
        {
            if (name.Length > suffix.Length
                && name.EndsWith(suffix, StringComparison.Ordinal)) {
                return name[..^suffix.Length];
            }

            return name;
        }
    }
}
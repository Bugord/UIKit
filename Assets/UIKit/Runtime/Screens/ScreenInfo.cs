using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace UIKit
{
    /// <summary>
    /// How a ViewModel type is identified and presented, from its <see cref="UIScreenAttribute"/>
    /// and the conventions described there.
    /// </summary>
    public sealed class ScreenInfo
    {
        private const string ViewModelSuffix = "ViewModel";

        private static readonly ConcurrentDictionary<Type, ScreenInfo> Cache = new();

        public string Id { get; }
        public UILayer Layer { get; }
        public DismissTriggers Dismiss { get; }

        private ScreenInfo(string id, UILayer layer, DismissTriggers dismiss)
        {
            Id = id;
            Layer = layer;
            Dismiss = dismiss;
        }

        public static ScreenInfo Of<TViewModel>() where TViewModel : ViewModel => Of(typeof(TViewModel));

        public static ScreenInfo Of(Type viewModelType)
        {
            if (viewModelType == null) {
                throw new ArgumentNullException(nameof(viewModelType));
            }

            return Cache.GetOrAdd(viewModelType, Create);
        }

        private static ScreenInfo Create(Type viewModelType)
        {
            var attribute = viewModelType.GetCustomAttribute<UIScreenAttribute>();

            var id = string.IsNullOrEmpty(attribute?.Id) ? DefaultId(viewModelType.Name) : attribute.Id;

            var layer = attribute?.LayerOverride
                ?? (typeof(IPopup).IsAssignableFrom(viewModelType) ? UILayer.Popup : UILayer.Screen);

            var dismiss = attribute?.DismissOverride
                ?? (layer == UILayer.Popup ? DismissTriggers.All : DismissTriggers.NavigationCancel);

            return new ScreenInfo(id, layer, dismiss);
        }

        private static string DefaultId(string typeName)
        {
            // A generic type's name carries its arity ("Foo`1"), which is no use as a file name.
            var tick = typeName.IndexOf('`');
            if (tick >= 0) {
                typeName = typeName[..tick];
            }

            return typeName.Length > ViewModelSuffix.Length && typeName.EndsWith(ViewModelSuffix, StringComparison.Ordinal)
                ? typeName[..^ViewModelSuffix.Length]
                : typeName;
        }
    }
}

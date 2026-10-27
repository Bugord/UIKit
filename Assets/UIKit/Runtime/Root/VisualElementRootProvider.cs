using System;
using UnityEngine.UIElements;

namespace UIKit
{
    /// <summary>
    /// Hosts the UI layers inside an element you already have, e.g. the root of your own
    /// <see cref="UIDocument"/>.
    /// </summary>
    public sealed class VisualElementRootProvider : IUIRootProvider
    {
        private readonly VisualElement screenRoot = LayerRoots.Create(LayerRoots.ScreenRootName);
        private readonly VisualElement popupRoot = LayerRoots.Create(LayerRoots.PopupRootName);

        public VisualElementRootProvider(VisualElement host)
        {
            if (host == null) {
                throw new ArgumentNullException(nameof(host));
            }

            // Added in this order so popups render above screens.
            host.Add(screenRoot);
            host.Add(popupRoot);
        }

        public VisualElement GetLayerRoot(UILayer layer) => layer switch {
            UILayer.Screen => screenRoot,
            UILayer.Popup => popupRoot,
            _ => throw new ArgumentOutOfRangeException(nameof(layer), layer, null)
        };
    }
}

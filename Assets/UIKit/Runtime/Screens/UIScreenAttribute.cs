using System;

namespace UIKit
{
    /// <summary>
    /// Overrides how a ViewModel's screen is identified and presented. Without it the id is the
    /// type name minus a trailing "ViewModel", the layer is <see cref="UILayer.Popup"/> for an
    /// <see cref="IPopup"/> and <see cref="UILayer.Screen"/> otherwise, and the dismiss
    /// triggers are <see cref="DismissTriggers.All"/> for popups and
    /// <see cref="DismissTriggers.NavigationCancel"/> for screens.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class UIScreenAttribute : Attribute
    {
        private UILayer? layer;
        private DismissTriggers? dismiss;

        public UIScreenAttribute(string id = null)
        {
            Id = id;
        }

        public UIScreenAttribute(UILayer layer, string id = null)
        {
            Id = id;
            this.layer = layer;
        }

        /// <summary>
        /// The screen id, which is also the catalog key and the UXML file name the catalog
        /// sync looks for.
        /// </summary>
        public string Id { get; }

        // The getters only report what was set here; ScreenInfo.Of gives the effective values.
        public UILayer Layer
        {
            get => layer ?? UILayer.Screen;
            set => layer = value;
        }

        public DismissTriggers Dismiss
        {
            get => dismiss ?? DismissTriggers.None;
            set => dismiss = value;
        }

        internal UILayer? LayerOverride => layer;
        internal DismissTriggers? DismissOverride => dismiss;
    }
}

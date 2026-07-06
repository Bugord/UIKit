using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer.Unity;

namespace UIKit.Runtime.Core
{
    public class UIRootProvider : IUIRootProvider, IInitializable
    {
        private const string RootObjectName = "UIKitRoot";
        private const string ScreenRootName = "ScreenRoot";
        private const string PopupsRootName = "PopupsRoot";

        private readonly PanelSettings panelSettings;

        private VisualElement screenRoot;
        private VisualElement popupsRoot;

        public UIRootProvider(PanelSettings panelSettings)
        {
            this.panelSettings = panelSettings;
        }

        public void Initialize()
        {
            var go = new GameObject(RootObjectName);
            UnityEngine.Object.DontDestroyOnLoad(go);

            var root = CreatePanelRoot(go);

            screenRoot = new VisualElement { name = ScreenRootName };
            popupsRoot = new VisualElement { name = PopupsRootName };
            root.Add(screenRoot);
            root.Add(popupsRoot);
        }

        public VisualElement GetLayerRoot(UILayer layer) =>
            layer switch {
                UILayer.Screen => screenRoot,
                UILayer.Popup => popupsRoot,
                _ => throw new ArgumentException($"Unknown UILayer: {layer}")
            };

#if UNITY_6000_5_OR_NEWER
        private VisualElement CreatePanelRoot(GameObject go)
        {
            var panelRenderer = go.AddComponent<PanelRenderer>();

            VisualElement root = null;
            panelRenderer.RegisterUIReloadCallback((_, reloadedRoot, _) => root = reloadedRoot);
            panelRenderer.panelSettings = panelSettings;

            if (root == null) {
                throw new InvalidOperationException(
                    $"{nameof(UIRootProvider)}: {nameof(PanelRenderer)} did not produce a root VisualElement.");
            }

            return root;
        }
#else
        private VisualElement CreatePanelRoot(GameObject go)
        {
            var uiDocument = go.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;

            if (uiDocument.rootVisualElement == null) {
                throw new InvalidOperationException(
                    $"{nameof(UIRootProvider)}: {nameof(UIDocument)} did not produce a root VisualElement.");
            }

            return uiDocument.rootVisualElement;
        }
#endif
    }
}

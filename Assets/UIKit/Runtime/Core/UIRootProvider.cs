using System;
using UIKit.Runtime.Utils;
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
            screenRoot = new VisualElement { name = ScreenRootName };
            popupsRoot = new VisualElement { name = PopupsRootName };
            screenRoot.StretchToParent();
            popupsRoot.StretchToParent();

            var go = new GameObject(RootObjectName);
            UnityEngine.Object.DontDestroyOnLoad(go);

            CreatePanelRoot(go);
        }

        public VisualElement GetLayerRoot(UILayer layer) =>
            layer switch {
                UILayer.Screen => screenRoot,
                UILayer.Popup => popupsRoot,
                _ => throw new ArgumentException($"Unknown UILayer: {layer}")
            };

#if UNITY_6000_5_OR_NEWER
        private void CreatePanelRoot(GameObject go)
        {
            var panelRenderer = go.AddComponent<PanelRenderer>();

            panelRenderer.RegisterUIReloadCallback(OnRegisterUIReload);
            panelRenderer.panelSettings = panelSettings;
        }

        private void OnRegisterUIReload(PanelRenderer renderer, VisualElement newRoot)
        {
            newRoot.Add(screenRoot);
            newRoot.Add(popupsRoot);
        }
#else
        private void CreatePanelRoot(GameObject go)
        {
            var uiDocument = go.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;

            var root = uiDocument.rootVisualElement;

            if (root == null) {
                throw new InvalidOperationException(
                    $"{nameof(UIRootProvider)}: {nameof(UIDocument)} did not produce a root VisualElement.");
            }

            root.Add(screenRoot);
            root.Add(popupsRoot);
        }
#endif
    }
}

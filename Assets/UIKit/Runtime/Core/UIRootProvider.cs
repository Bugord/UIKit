using System;
using UIKit.Runtime.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace UIKit.Runtime.Core
{
    public class UIRootProvider : IUIRootProvider, IDisposable
    {
        private const string RootObjectName = "UIKitRoot";
        private const string ScreenRootName = "ScreenRoot";
        private const string PopupsRootName = "PopupsRoot";

        private readonly PanelSettings panelSettings;

        private VisualElement screenRoot;
        private VisualElement popupsRoot;
        private GameObject rootObject;

        public UIRootProvider(PanelSettings panelSettings)
        {
            this.panelSettings = panelSettings;
        }

        public void Initialize()
        {
            // The root object survives scene loads, so a scope rebuilt by a scene reload
            // would otherwise stack a second UIKitRoot and a second panel on top of it.
            if (rootObject != null) {
                return;
            }

            screenRoot = new VisualElement { name = ScreenRootName };
            popupsRoot = new VisualElement { name = PopupsRootName };
            screenRoot.StretchToParent();
            popupsRoot.StretchToParent();

            rootObject = new GameObject(RootObjectName);
            UnityEngine.Object.DontDestroyOnLoad(rootObject);

            CreatePanelRoot(rootObject);
        }

        public void Dispose()
        {
            if (rootObject != null) {
                if (Application.isPlaying) {
                    UnityEngine.Object.Destroy(rootObject);
                } else {
                    UnityEngine.Object.DestroyImmediate(rootObject);
                }
            }

            rootObject = null;
            screenRoot = null;
            popupsRoot = null;
        }

        public VisualElement GetLayerRoot(UILayer layer)
        {
            if (rootObject == null) {
                throw new InvalidOperationException(
                    $"{nameof(UIRootProvider)}: {nameof(Initialize)} has not run (or the provider " +
                    "was disposed). Register UIKit before resolving the navigator.");
            }

            return layer switch {
                UILayer.Screen => screenRoot,
                UILayer.Popup => popupsRoot,
                _ => throw new ArgumentException($"Unknown UILayer: {layer}")
            };
        }

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

            go.AddComponent<UIDocumentRootKeeper>().Keep(uiDocument, screenRoot, popupsRoot);
        }
#endif
    }
}
